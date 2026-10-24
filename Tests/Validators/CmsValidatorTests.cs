using Domain.Constants;
using FluentValidation;
using Service.Helpers.DTOs.AboutPillars;
using Service.Helpers.DTOs.Abouts;
using Service.Helpers.DTOs.BenefitItems;
using Service.Helpers.DTOs.BenefitSections;
using Service.Helpers.DTOs.Brands;
using Service.Helpers.DTOs.CardDesigns;
using Service.Helpers.DTOs.CardHeroes;
using Service.Helpers.DTOs.HomeTickers;
using Service.Helpers.DTOs.ServiceItems;
using Service.Helpers.DTOs.ServiceSections;
using Service.Helpers.DTOs.Settings;
using Service.Helpers.Validators;
using Service.Helpers.Validators.AboutPillars;
using Service.Helpers.Validators.Abouts;
using Service.Helpers.Validators.BenefitItems;
using Service.Helpers.Validators.BenefitSections;
using Service.Helpers.Validators.Brands;
using Service.Helpers.Validators.CardDesigns;
using Service.Helpers.Validators.CardHeroes;
using Service.Helpers.Validators.HomeTickers;
using Service.Helpers.Validators.ServiceItems;
using Service.Helpers.Validators.ServiceSections;
using Service.Helpers.Validators.Settings;
using Xunit;

namespace CaspianBank.Tests.Validators
{
    public class CmsValidatorTests
    {
        // Mətn sahələrinin ortaq qaydası: boş olmaz, kənar boşluqlardan sonra limiti keçməz
        private static void CheckText<T>(IValidator<T> validator, Func<string?, T> build, int limit)
        {
            Assert.True(V.IsValid(validator, build("ok")));
            Assert.True(V.IsValid(validator, build(V.Text(limit))));
            Assert.True(V.IsValid(validator, build(V.Text(limit) + "   ")));          // boşluqlar sayılmır
            Assert.False(V.IsValid(validator, build(V.Text(limit + 1))));
            Assert.False(V.IsValid(validator, build(null)));
            Assert.False(V.IsValid(validator, build("")));
            Assert.False(V.IsValid(validator, build("    ")));
        }

        // ---------- About ----------

        [Fact]
        public void About_texts_follow_the_column_lengths()
        {
            var validator = new UpdateAboutDtoValidator();
            CheckText(validator, v => new AboutUpdateDto { Label = v, Title = "t", Description = "d" }, 100);
            CheckText(validator, v => new AboutUpdateDto { Label = "l", Title = v, Description = "d" }, 200);
            CheckText(validator, v => new AboutUpdateDto { Label = "l", Title = "t", Description = v }, 500);
        }

        [Fact]
        public void AboutVideo_needs_a_non_empty_file()
        {
            var validator = new UpdateAboutVideoDtoValidator();
            Assert.True(V.IsValid(validator, new UpdateAboutVideoDto { Video = V.File("v.mp4") }));
            Assert.False(V.IsValid(validator, new UpdateAboutVideoDto { Video = null }));
            Assert.False(V.IsValid(validator, new UpdateAboutVideoDto { Video = V.EmptyFile() }));
        }

        // ---------- About pillars ----------

        [Fact]
        public void CreateAboutPillar_needs_title_description_and_image()
        {
            var validator = new CreateAboutPillarDtoValidator();
            CheckText(validator, v => new CreateAboutPillarDto { Title = v, Description = "d", Image = V.File() }, 100);
            CheckText(validator, v => new CreateAboutPillarDto { Title = "t", Description = v, Image = V.File() }, 300);
            Assert.False(V.IsValid(validator, new CreateAboutPillarDto { Title = "t", Description = "d", Image = null }));
            Assert.False(V.IsValid(validator, new CreateAboutPillarDto { Title = "t", Description = "d", Image = V.EmptyFile() }));
        }

        [Fact]
        public void UpdateAboutPillar_image_is_optional()
        {
            var validator = new UpdateAboutPillarDtoValidator();
            CheckText(validator, v => new UpdateAboutPillarDto { Title = v, Description = "d" }, 100);
            CheckText(validator, v => new UpdateAboutPillarDto { Title = "t", Description = v }, 300);
            Assert.True(V.IsValid(validator, new UpdateAboutPillarDto { Title = "t", Description = "d", Image = null }));
        }

        // ---------- Benefit ----------

        private static string FirstDestination()
        {
            return ButtonDestinations.All[0].Path;
        }

        [Fact]
        public void BenefitItem_create_and_update_check_every_text()
        {
            var create = new CreateBenefitItemDtoValidator();
            var update = new UpdateBenefitItemDtoValidator();
            var path = FirstDestination();

            CheckText(create, v => new BenefitItemCreateDto { Label = v, Title = "t", Description = "d", ButtonText = "b", ButtonUrl = path, Text1 = "1", Text2 = "2", Text3 = "3" }, 100);
            CheckText(create, v => new BenefitItemCreateDto { Label = "l", Title = v, Description = "d", ButtonText = "b", ButtonUrl = path, Text1 = "1", Text2 = "2", Text3 = "3" }, 200);
            CheckText(create, v => new BenefitItemCreateDto { Label = "l", Title = "t", Description = v, ButtonText = "b", ButtonUrl = path, Text1 = "1", Text2 = "2", Text3 = "3" }, 300);
            CheckText(create, v => new BenefitItemCreateDto { Label = "l", Title = "t", Description = "d", ButtonText = v, ButtonUrl = path, Text1 = "1", Text2 = "2", Text3 = "3" }, 100);
            CheckText(create, v => new BenefitItemCreateDto { Label = "l", Title = "t", Description = "d", ButtonText = "b", ButtonUrl = path, Text1 = v, Text2 = "2", Text3 = "3" }, 200);
            CheckText(create, v => new BenefitItemCreateDto { Label = "l", Title = "t", Description = "d", ButtonText = "b", ButtonUrl = path, Text1 = "1", Text2 = v, Text3 = "3" }, 200);
            CheckText(create, v => new BenefitItemCreateDto { Label = "l", Title = "t", Description = "d", ButtonText = "b", ButtonUrl = path, Text1 = "1", Text2 = "2", Text3 = v }, 200);

            CheckText(update, v => new BenefitItemUpdateDto { Label = v, Title = "t", Description = "d", ButtonText = "b", ButtonUrl = path, Text1 = "1", Text2 = "2", Text3 = "3" }, 100);
            CheckText(update, v => new BenefitItemUpdateDto { Label = "l", Title = v, Description = "d", ButtonText = "b", ButtonUrl = path, Text1 = "1", Text2 = "2", Text3 = "3" }, 200);
            CheckText(update, v => new BenefitItemUpdateDto { Label = "l", Title = "t", Description = v, ButtonText = "b", ButtonUrl = path, Text1 = "1", Text2 = "2", Text3 = "3" }, 300);
        }

        [Fact]
        public void BenefitItem_button_must_go_to_a_known_destination()
        {
            var create = new CreateBenefitItemDtoValidator();
            var update = new UpdateBenefitItemDtoValidator();
            foreach (var (path, _) in ButtonDestinations.All)
            {
                Assert.True(V.IsValid(create, new BenefitItemCreateDto { Label = "l", Title = "t", Description = "d", ButtonText = "b", ButtonUrl = path, Text1 = "1", Text2 = "2", Text3 = "3" }));
                Assert.True(V.IsValid(update, new BenefitItemUpdateDto { Label = "l", Title = "t", Description = "d", ButtonText = "b", ButtonUrl = path, Text1 = "1", Text2 = "2", Text3 = "3" }));
            }
            Assert.False(V.IsValid(create, new BenefitItemCreateDto { Label = "l", Title = "t", Description = "d", ButtonText = "b", ButtonUrl = "https://evil.example", Text1 = "1", Text2 = "2", Text3 = "3" }));
            Assert.False(V.IsValid(update, new BenefitItemUpdateDto { Label = "l", Title = "t", Description = "d", ButtonText = "b", ButtonUrl = "javascript:alert(1)", Text1 = "1", Text2 = "2", Text3 = "3" }));
            Assert.False(V.IsValid(update, new BenefitItemUpdateDto { Label = "l", Title = "t", Description = "d", ButtonText = "b", ButtonUrl = null, Text1 = "1", Text2 = "2", Text3 = "3" }));
        }

        [Fact]
        public void ButtonDestinations_Contains_trims_and_is_case_sensitive()
        {
            Assert.True(ButtonDestinations.Contains("  /App  "));
            Assert.False(ButtonDestinations.Contains("/app"));
            Assert.False(ButtonDestinations.Contains(null));
            Assert.False(ButtonDestinations.Contains(""));
        }

        [Fact]
        public void BenefitSection_texts_follow_the_column_lengths()
        {
            var validator = new UpdateBenefitSectionDtoValidator();
            CheckText(validator, v => new BenefitSectionUpdateDto { Label = v, Title = "t", Description = "d" }, 100);
            CheckText(validator, v => new BenefitSectionUpdateDto { Label = "l", Title = v, Description = "d" }, 200);
            CheckText(validator, v => new BenefitSectionUpdateDto { Label = "l", Title = "t", Description = v }, 500);
        }

        // ---------- Brands ----------

        [Fact]
        public void CreateBrand_needs_name_and_image()
        {
            var validator = new CreateBrandDtoValidator();
            CheckText(validator, v => new CreateBrandDto { Name = v, Image = V.File() }, 100);
            Assert.False(V.IsValid(validator, new CreateBrandDto { Name = "n", Image = null }));
            Assert.False(V.IsValid(validator, new CreateBrandDto { Name = "n", Image = V.EmptyFile() }));
        }

        [Fact]
        public void UpdateBrand_image_is_optional()
        {
            var validator = new UpdateBrandDtoValidator();
            CheckText(validator, v => new UpdateBrandDto { Name = v }, 100);
            Assert.True(V.IsValid(validator, new UpdateBrandDto { Name = "n", Image = null }));
        }

        // ---------- Card designs ----------

        [Fact]
        public void CardDesign_title_order_and_image_are_checked()
        {
            var create = new CreateCardDesignDtoValidator();
            var update = new UpdateCardDesignDtoValidator();

            CheckText(create, v => new CreateCardDesignDto { Title = v, DisplayOrder = 1, Image = V.File() }, 50);
            CheckText(update, v => new UpdateCardDesignDto { Title = v, DisplayOrder = 1 }, 50);

            Assert.True(V.IsValid(create, new CreateCardDesignDto { Title = "t", DisplayOrder = 0, Image = V.File() }));
            Assert.True(V.IsValid(create, new CreateCardDesignDto { Title = "t", DisplayOrder = 1000, Image = V.File() }));
            Assert.False(V.IsValid(create, new CreateCardDesignDto { Title = "t", DisplayOrder = -1, Image = V.File() }));
            Assert.False(V.IsValid(create, new CreateCardDesignDto { Title = "t", DisplayOrder = 1001, Image = V.File() }));
            Assert.False(V.IsValid(create, new CreateCardDesignDto { Title = "t", DisplayOrder = 1, Image = null }));
            Assert.False(V.IsValid(update, new UpdateCardDesignDto { Title = "t", DisplayOrder = 1001 }));
            Assert.True(V.IsValid(update, new UpdateCardDesignDto { Title = "t", DisplayOrder = 5, Image = null }));
        }

        // ---------- Hero, tickers, services ----------

        [Fact]
        public void CardHero_texts_follow_the_column_lengths()
        {
            var validator = new UpdateCardHeroDtoValidator();
            CheckText(validator, v => new UpdateCardHeroDto { Label = v, Title = "t", Description = "d" }, 100);
            CheckText(validator, v => new UpdateCardHeroDto { Label = "l", Title = v, Description = "d" }, 200);
            CheckText(validator, v => new UpdateCardHeroDto { Label = "l", Title = "t", Description = v }, 500);
        }

        [Fact]
        public void HomeTicker_text_is_at_most_100_characters()
        {
            CheckText(new CreateHomeTickerDtoValidator(), v => new CreateHomeTickerDto { Text = v }, 100);
            CheckText(new UpdateHomeTickerDtoValidator(), v => new UpdateHomeTickerDto { Text = v }, 100);
        }

        [Fact]
        public void ServiceItem_texts_follow_the_column_lengths()
        {
            var validator = new UpdateServiceItemDtoValidator();
            CheckText(validator, v => new ServiceItemUpdateDto { Title = v, Description = "d" }, 100);
            CheckText(validator, v => new ServiceItemUpdateDto { Title = "t", Description = v }, 300);
        }

        [Fact]
        public void ServiceSection_texts_follow_the_column_lengths()
        {
            var validator = new UpdateServiceSectionDtoValidator();
            CheckText(validator, v => new ServiceSectionUpdateDto { Label = v, Title = "t", Description = "d" }, 100);
            CheckText(validator, v => new ServiceSectionUpdateDto { Label = "l", Title = v, Description = "d" }, 200);
            CheckText(validator, v => new ServiceSectionUpdateDto { Label = "l", Title = "t", Description = v }, 500);
        }

        // ---------- Settings ----------

        private static UpdateSettingDto Setting(string key, string? value)
        {
            return new UpdateSettingDto { Key = key, Value = value };
        }

        [Fact]
        public void Setting_value_cannot_be_empty_or_longer_than_500_characters()
        {
            var validator = new UpdateSettingDtoValidator();
            Assert.False(V.IsValid(validator, Setting(SettingKeys.FooterDescription, "")));
            Assert.False(V.IsValid(validator, Setting(SettingKeys.FooterDescription, null)));
            Assert.True(V.IsValid(validator, Setting(SettingKeys.FooterDescription, V.Text(500))));
            Assert.False(V.IsValid(validator, Setting(SettingKeys.FooterDescription, V.Text(501))));
        }

        [Fact]
        public void Setting_specific_limits_apply_by_key()
        {
            var validator = new UpdateSettingDtoValidator();
            Assert.True(V.IsValid(validator, Setting(SettingKeys.CompanyName, V.Text(100))));
            Assert.False(V.IsValid(validator, Setting(SettingKeys.CompanyName, V.Text(101))));
            Assert.True(V.IsValid(validator, Setting(SettingKeys.Address, V.Text(200))));
            Assert.False(V.IsValid(validator, Setting(SettingKeys.Address, V.Text(201))));
            Assert.False(V.IsValid(validator, Setting(SettingKeys.FooterTitle, V.Text(201))));
            Assert.False(V.IsValid(validator, Setting(SettingKeys.Copyright, V.Text(101))));
        }

        [Fact]
        public void Setting_email_must_be_an_address()
        {
            var validator = new UpdateSettingDtoValidator();
            Assert.True(V.IsValid(validator, Setting(SettingKeys.Email, "support@caspianbank.az")));
            Assert.False(V.IsValid(validator, Setting(SettingKeys.Email, "not-an-email")));
            Assert.False(V.IsValid(validator, Setting(SettingKeys.Email, V.Text(96) + "@b.az")));
        }

        [Theory]
        [InlineData("+994 12 345 67 89", true)]
        [InlineData("0777610606", true)]
        [InlineData("012-310-00-00", true)]
        [InlineData("012 (310) 00 00", true)]
        [InlineData("12345", false)]
        [InlineData("call me", false)]
        public void Setting_phone_must_look_like_a_phone_number(string phone, bool valid)
        {
            Assert.Equal(valid, V.IsValid(new UpdateSettingDtoValidator(), Setting(SettingKeys.PhoneNumber, phone)));
        }

        [Theory]
        [InlineData("https://www.instagram.com/efendiyev.006/", true)]
        [InlineData("http://example.com", true)]
        [InlineData("javascript:alert(1)", false)]
        [InlineData("ftp://example.com", false)]
        [InlineData("instagram.com/x", false)]
        [InlineData("", false)]
        public void Setting_social_links_must_be_http_or_https(string link, bool valid)
        {
            var validator = new UpdateSettingDtoValidator();
            Assert.Equal(valid, V.IsValid(validator, Setting(SettingKeys.InstagramUrl, link)));
            Assert.Equal(valid, V.IsValid(validator, Setting(SettingKeys.FacebookUrl, link)));
            Assert.Equal(valid, V.IsValid(validator, Setting(SettingKeys.TwitterUrl, link)));
            Assert.Equal(valid, V.IsValid(validator, Setting(SettingKeys.LinkedInUrl, link)));
        }

        [Fact]
        public void Setting_social_link_is_at_most_200_characters()
        {
            var link = "https://example.com/" + V.Text(190);
            Assert.False(V.IsValid(new UpdateSettingDtoValidator(), Setting(SettingKeys.InstagramUrl, link)));
        }

        [Theory]
        [InlineData("40.3730", true)]
        [InlineData("-33.86", true)]
        [InlineData("90", true)]
        [InlineData("-90", true)]
        [InlineData("90.0001", false)]
        [InlineData("40,3730", false)]       // vergüllə yox, nöqtə ilə
        [InlineData("abc", false)]
        public void Setting_map_latitude_is_a_number_between_minus_90_and_90(string value, bool valid)
        {
            Assert.Equal(valid, V.IsValid(new UpdateSettingDtoValidator(), Setting(SettingKeys.MapLatitude, value)));
        }

        [Theory]
        [InlineData("49.8370", true)]
        [InlineData("-180", true)]
        [InlineData("180", true)]
        [InlineData("180.01", false)]
        [InlineData("-181", false)]
        [InlineData("49,8370", false)]
        [InlineData("", false)]
        public void Setting_map_longitude_is_a_number_between_minus_180_and_180(string value, bool valid)
        {
            Assert.Equal(valid, V.IsValid(new UpdateSettingDtoValidator(), Setting(SettingKeys.MapLongitude, value)));
        }

        [Fact]
        public void SettingLogo_needs_a_non_empty_file()
        {
            var validator = new UpdateSettingLogoDtoValidator();
            Assert.True(V.IsValid(validator, new UpdateSettingLogoDto { Image = V.File() }));
            Assert.False(V.IsValid(validator, new UpdateSettingLogoDto { Image = null }));
            Assert.False(V.IsValid(validator, new UpdateSettingLogoDto { Image = V.EmptyFile() }));
        }

        // ---------- Ortaq köməkçilər ----------

        private class Holder
        {
            public string? Text { get; set; }
        }

        private class HolderValidator : AbstractValidator<Holder>
        {
            public HolderValidator()
            {
                RuleFor(x => x.Text).RequiredText("text", 5);
            }
        }

        [Fact]
        public async Task EnsureValidAsync_throws_BadRequest_with_distinct_messages()
        {
            var validator = new HolderValidator();
            var error = await Assert.ThrowsAsync<Service.Helpers.Exceptions.BadRequestException>(() => validator.EnsureValidAsync(new Holder { Text = "" }));
            Assert.Contains("Enter the text.", error.Errors);
        }

        [Fact]
        public async Task EnsureValidAsync_passes_a_valid_model()
        {
            await new HolderValidator().EnsureValidAsync(new Holder { Text = "ok" });
        }
    }
}
