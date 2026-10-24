using CaspianBank.Tests.Infrastructure;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Repository.Data;
using Service.Helpers.DTOs.HomeTickers;
using Service.Helpers.DTOs.Translations;
using Service.Services.Interfaces;
using Xunit;

namespace CaspianBank.Tests.Services
{
    // Admin CMS səhifəsində dilə görə mətn əlavə etmə/yeniləmə və ingilis mətn dəyişəndə tərcümənin onu izləməsi
    public class ContentTranslationTests
    {
        private static async Task<T> WithAsync<T>(Func<ITranslationService, Task<T>> action)
        {
            var app = await TestApp.SharedAsync();
            using var scope = app.NewScope();
            return await action(scope.ServiceProvider.GetRequiredService<ITranslationService>());
        }

        private static async Task<TResult> WithTickerAsync<TResult>(Func<IHomeTickerService, Task<TResult>> action)
        {
            var app = await TestApp.SharedAsync();
            using var scope = app.NewScope();
            return await action(scope.ServiceProvider.GetRequiredService<IHomeTickerService>());
        }

        private static string Unique(string prefix)
        {
            return prefix + " " + Guid.NewGuid().ToString("N")[..10];
        }

        [Fact]
        public async Task A_language_can_be_saved_and_read_back_for_several_texts()
        {
            var one = Unique("Fast transfers");
            var two = Unique("Open all day");

            var saved = await WithAsync(s => s.SaveContentAsync(new SaveContentTranslationsDto
            {
                Language = "az",
                Items = new List<ContentTranslationItemDto>
                {
                    new ContentTranslationItemDto { Key = one, Value = "  Sürətli köçürmələr  " },
                    new ContentTranslationItemDto { Key = two, Value = "Bütün gün açıq" }
                }
            }));

            Assert.True(saved.IsSuccess);
            Assert.Equal(2, saved.Data);

            var read = await WithAsync(s => s.LookupAsync(new LookupTranslationsDto { Language = "az", Keys = new List<string> { one, two, "Never translated " + one } }));
            Assert.True(read.IsSuccess);
            Assert.Equal("Sürətli köçürmələr", read.Data![one]);
            Assert.Equal("Bütün gün açıq", read.Data[two]);
            Assert.Equal(2, read.Data.Count);
        }

        [Fact]
        public async Task Saving_one_language_leaves_the_other_language_alone()
        {
            var key = Unique("Pay your bills");
            await WithAsync(s => s.SaveContentAsync(new SaveContentTranslationsDto { Language = "ru", Items = new List<ContentTranslationItemDto> { new ContentTranslationItemDto { Key = key, Value = "Оплачивайте счета" } } }));
            await WithAsync(s => s.SaveContentAsync(new SaveContentTranslationsDto { Language = "az", Items = new List<ContentTranslationItemDto> { new ContentTranslationItemDto { Key = key, Value = "Ödənişləri edin" } } }));

            var ru = await WithAsync(s => s.LookupAsync(new LookupTranslationsDto { Language = "ru", Keys = new List<string> { key } }));
            var az = await WithAsync(s => s.LookupAsync(new LookupTranslationsDto { Language = "az", Keys = new List<string> { key } }));

            Assert.Equal("Оплачивайте счета", ru.Data![key]);
            Assert.Equal("Ödənişləri edin", az.Data![key]);
        }

        [Fact]
        public async Task An_empty_value_removes_the_translation()
        {
            var key = Unique("Secure cards");
            await WithAsync(s => s.SaveContentAsync(new SaveContentTranslationsDto { Language = "az", Items = new List<ContentTranslationItemDto> { new ContentTranslationItemDto { Key = key, Value = "Təhlükəsiz kartlar" } } }));
            await WithAsync(s => s.SaveContentAsync(new SaveContentTranslationsDto { Language = "az", Items = new List<ContentTranslationItemDto> { new ContentTranslationItemDto { Key = key, Value = "  " } } }));

            var read = await WithAsync(s => s.LookupAsync(new LookupTranslationsDto { Language = "az", Keys = new List<string> { key } }));

            Assert.Empty(read.Data!);
        }

        [Fact]
        public async Task Only_az_and_ru_are_accepted_and_the_input_is_checked()
        {
            var badLanguage = await WithAsync(s => s.SaveContentAsync(new SaveContentTranslationsDto { Language = "en", Items = new List<ContentTranslationItemDto> { new ContentTranslationItemDto { Key = "x", Value = "y" } } }));
            var badLookup = await WithAsync(s => s.LookupAsync(new LookupTranslationsDto { Language = "fr", Keys = new List<string> { "x" } }));
            var emptyKey = await WithAsync(s => s.SaveContentAsync(new SaveContentTranslationsDto { Language = "az", Items = new List<ContentTranslationItemDto> { new ContentTranslationItemDto { Key = " ", Value = "y" } } }));
            var tooLong = await WithAsync(s => s.SaveContentAsync(new SaveContentTranslationsDto { Language = "az", Items = new List<ContentTranslationItemDto> { new ContentTranslationItemDto { Key = "k", Value = new string('a', 2001) } } }));
            var tooMany = await WithAsync(s => s.SaveContentAsync(new SaveContentTranslationsDto { Language = "az", Items = Enumerable.Range(0, 51).Select(i => new ContentTranslationItemDto { Key = "k" + i, Value = "v" }).ToList() }));

            Assert.False(badLanguage.IsSuccess);
            Assert.False(badLookup.IsSuccess);
            Assert.False(emptyKey.IsSuccess);
            Assert.Equal("A translation can be at most 2000 characters.", tooLong.Errors[0]);
            Assert.False(tooMany.IsSuccess);
        }

        [Fact]
        public async Task Changing_the_english_text_moves_the_translations_to_the_new_text()
        {
            var oldText = Unique("Always open");
            var newText = Unique("Open round the clock");
            await WithTickerAsync(s => s.CreateAsync(new CreateHomeTickerDto { Text = oldText }).ContinueWith(_ => 0));
            var ticker = (await WithTickerAsync(s => s.GetAllAsync())).Single(t => t.Text == oldText);
            await WithAsync(s => s.SaveContentAsync(new SaveContentTranslationsDto
            {
                Language = "az",
                Items = new List<ContentTranslationItemDto> { new ContentTranslationItemDto { Key = oldText, Value = "Həmişə açıq" } }
            }));
            await WithAsync(s => s.SaveContentAsync(new SaveContentTranslationsDto
            {
                Language = "ru",
                Items = new List<ContentTranslationItemDto> { new ContentTranslationItemDto { Key = oldText, Value = "Всегда открыто" } }
            }));

            await WithTickerAsync(s => s.UpdateAsync(ticker.Id, new UpdateHomeTickerDto { Text = newText }).ContinueWith(_ => 0));

            var az = await WithAsync(s => s.LookupAsync(new LookupTranslationsDto { Language = "az", Keys = new List<string> { oldText, newText } }));
            var ru = await WithAsync(s => s.LookupAsync(new LookupTranslationsDto { Language = "ru", Keys = new List<string> { oldText, newText } }));
            Assert.Equal("Həmişə açıq", az.Data![newText]);
            Assert.Equal("Всегда открыто", ru.Data![newText]);
            Assert.False(az.Data.ContainsKey(oldText));
            Assert.False(ru.Data.ContainsKey(oldText));
        }

        [Fact]
        public async Task A_translation_that_already_exists_for_the_new_text_is_not_overwritten()
        {
            var oldText = Unique("Quick loans");
            var newText = Unique("Fast loans");
            await WithTickerAsync(s => s.CreateAsync(new CreateHomeTickerDto { Text = oldText }).ContinueWith(_ => 0));
            var ticker = (await WithTickerAsync(s => s.GetAllAsync())).Single(t => t.Text == oldText);
            await WithAsync(s => s.SaveContentAsync(new SaveContentTranslationsDto
            {
                Language = "az",
                Items = new List<ContentTranslationItemDto>
                {
                    new ContentTranslationItemDto { Key = oldText, Value = "Sürətli kreditlər (köhnə)" },
                    new ContentTranslationItemDto { Key = newText, Value = "Tez kreditlər" }
                }
            }));

            await WithTickerAsync(s => s.UpdateAsync(ticker.Id, new UpdateHomeTickerDto { Text = newText }).ContinueWith(_ => 0));

            var az = await WithAsync(s => s.LookupAsync(new LookupTranslationsDto { Language = "az", Keys = new List<string> { oldText, newText } }));
            Assert.Equal("Tez kreditlər", az.Data![newText]);
            Assert.Equal("Sürətli kreditlər (köhnə)", az.Data[oldText]);
        }

        [Fact]
        public async Task A_change_that_only_differs_in_case_does_not_touch_the_translations()
        {
            var oldText = Unique("open every day");
            await WithTickerAsync(s => s.CreateAsync(new CreateHomeTickerDto { Text = oldText }).ContinueWith(_ => 0));
            var ticker = (await WithTickerAsync(s => s.GetAllAsync())).Single(t => t.Text == oldText);
            await WithAsync(s => s.SaveContentAsync(new SaveContentTranslationsDto
            {
                Language = "az",
                Items = new List<ContentTranslationItemDto> { new ContentTranslationItemDto { Key = oldText, Value = "hər gün açıq" } }
            }));

            await WithTickerAsync(s => s.UpdateAsync(ticker.Id, new UpdateHomeTickerDto { Text = oldText.ToUpperInvariant() }).ContinueWith(_ => 0));

            var az = await WithAsync(s => s.LookupAsync(new LookupTranslationsDto { Language = "az", Keys = new List<string> { oldText } }));
            Assert.Equal("hər gün açıq", az.Data![oldText]);
        }
    }
}
