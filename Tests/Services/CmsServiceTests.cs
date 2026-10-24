using CaspianBank.Tests.Infrastructure;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Service.Helpers.DTOs.AboutPillars;
using Service.Helpers.DTOs.Abouts;
using Service.Helpers.DTOs.BenefitItems;
using Service.Helpers.DTOs.BenefitSections;
using Service.Helpers.DTOs.Brands;
using Service.Helpers.DTOs.CardDesigns;
using Service.Helpers.DTOs.CardHeroes;
using Service.Helpers.DTOs.CardTiers;
using Service.Helpers.DTOs.Cards;
using Service.Helpers.DTOs.HomeTickers;
using Service.Helpers.DTOs.ServiceItems;
using Service.Helpers.DTOs.ServiceSections;
using Service.Helpers.DTOs.Settings;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;
using Xunit;

namespace CaspianBank.Tests.Services
{
    // Home səhifəsinin məzmun xidmətləri (admin paneldən idarə olunan mətnlər, şəkillər, düymələr)
    public class CmsServiceTests : IClassFixture<IsolatedApp>
    {
        private readonly TestApp _app;

        public CmsServiceTests(IsolatedApp fixture)
        {
            _app = fixture.App;
        }

        private async Task<T> WithAsync<T, TService>(Func<TService, Task<T>> action) where TService : notnull
        {
            using var scope = _app.NewScope();
            return await action(scope.ServiceProvider.GetRequiredService<TService>());
        }

        private Task WithAsync<TService>(Func<TService, Task> action) where TService : notnull
        {
            return WithAsync<int, TService>(async s =>
            {
                await action(s);
                return 0;
            });
        }

        private string PathOnDisk(string webPath)
        {
            return Path.Combine(_app.WebRoot, webPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        }

        private static Microsoft.AspNetCore.Http.IFormFile Image()
        {
            return FileServiceTests.Png();
        }

        private async Task<About> EnsureAboutAsync()
        {
            return await _app.QueryAsync(async db =>
            {
                var about = await db.Abouts.FirstOrDefaultAsync();
                if (about is null)
                {
                    about = new About { Label = "About", Title = "About title", Description = "About text", VideoPath = "/videos/about/homeVideo.mp4" };
                    db.Abouts.Add(about);
                    await db.SaveChangesAsync();
                }
                return about;
            });
        }

        // ---------- Brendlər ----------

        [Fact]
        public async Task Brand_create_saves_the_image_and_the_trimmed_name()
        {
            await WithAsync<IBrandService>(s => s.CreateAsync(new CreateBrandDto { Name = "  SeaBreeze  ", Image = Image() }));

            var brand = (await WithAsync<IEnumerable<BrandDto>, IBrandService>(s => s.GetAllAsync())).Single(b => b.Name == "SeaBreeze");
            Assert.Matches(@"^/images/brands/[0-9a-f]{32}\.png$", brand.Image);
            Assert.True(File.Exists(PathOnDisk(brand.Image)));
        }

        [Fact]
        public async Task Brand_create_validates_before_saving_a_file()
        {
            var before = Directory.Exists(Path.Combine(_app.WebRoot, "images", "brands")) ? Directory.GetFiles(Path.Combine(_app.WebRoot, "images", "brands")).Length : 0;

            await Assert.ThrowsAsync<BadRequestException>(() => WithAsync<IBrandService>(s => s.CreateAsync(new CreateBrandDto { Name = "", Image = Image() })));
            await Assert.ThrowsAsync<BadRequestException>(() => WithAsync<IBrandService>(s => s.CreateAsync(new CreateBrandDto { Name = "Valid", Image = null })));

            var after = Directory.Exists(Path.Combine(_app.WebRoot, "images", "brands")) ? Directory.GetFiles(Path.Combine(_app.WebRoot, "images", "brands")).Length : 0;
            Assert.Equal(before, after);
        }

        [Fact]
        public async Task Brand_update_replaces_the_image_and_removes_the_old_file()
        {
            await WithAsync<IBrandService>(s => s.CreateAsync(new CreateBrandDto { Name = "Updatable", Image = Image() }));
            var brand = (await WithAsync<IEnumerable<BrandDto>, IBrandService>(s => s.GetAllAsync())).Single(b => b.Name == "Updatable");
            var oldFile = PathOnDisk(brand.Image);

            await WithAsync<IBrandService>(s => s.UpdateAsync(brand.Id, new UpdateBrandDto { Name = "  Updated  ", Image = Image() }));

            var updated = await WithAsync<BrandDto, IBrandService>(s => s.GetDetailAsync(brand.Id));
            Assert.Equal("Updated", updated.Name);
            Assert.NotEqual(brand.Image, updated.Image);
            Assert.False(File.Exists(oldFile));
            Assert.True(File.Exists(PathOnDisk(updated.Image)));
        }

        [Fact]
        public async Task Brand_update_without_an_image_keeps_the_old_one()
        {
            await WithAsync<IBrandService>(s => s.CreateAsync(new CreateBrandDto { Name = "KeepImage", Image = Image() }));
            var brand = (await WithAsync<IEnumerable<BrandDto>, IBrandService>(s => s.GetAllAsync())).Single(b => b.Name == "KeepImage");

            await WithAsync<IBrandService>(s => s.UpdateAsync(brand.Id, new UpdateBrandDto { Name = "Renamed", Image = null }));

            var updated = await WithAsync<BrandDto, IBrandService>(s => s.GetDetailAsync(brand.Id));
            Assert.Equal("Renamed", updated.Name);
            Assert.Equal(brand.Image, updated.Image);
            Assert.True(File.Exists(PathOnDisk(updated.Image)));
        }

        [Fact]
        public async Task Brand_update_with_a_bad_image_keeps_the_old_image()
        {
            await WithAsync<IBrandService>(s => s.CreateAsync(new CreateBrandDto { Name = "BadUpdate", Image = Image() }));
            var brand = (await WithAsync<IEnumerable<BrandDto>, IBrandService>(s => s.GetAllAsync())).Single(b => b.Name == "BadUpdate");

            await Assert.ThrowsAsync<BadRequestException>(() => WithAsync<IBrandService>(s => s.UpdateAsync(brand.Id, new UpdateBrandDto { Name = "X", Image = FileServiceTests.Make(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }) })));

            var after = await WithAsync<BrandDto, IBrandService>(s => s.GetDetailAsync(brand.Id));
            Assert.Equal("BadUpdate", after.Name);
            Assert.Equal(brand.Image, after.Image);
            Assert.True(File.Exists(PathOnDisk(after.Image)));
        }

        [Fact]
        public async Task Brand_delete_removes_the_row_and_the_file()
        {
            await WithAsync<IBrandService>(s => s.CreateAsync(new CreateBrandDto { Name = "Deletable", Image = Image() }));
            var brand = (await WithAsync<IEnumerable<BrandDto>, IBrandService>(s => s.GetAllAsync())).Single(b => b.Name == "Deletable");

            await WithAsync<IBrandService>(s => s.DeleteAsync(brand.Id));

            Assert.DoesNotContain(await WithAsync<IEnumerable<BrandDto>, IBrandService>(s => s.GetAllAsync()), b => b.Id == brand.Id);
            Assert.False(File.Exists(PathOnDisk(brand.Image)));
        }

        [Fact]
        public async Task Brand_actions_on_an_unknown_id_throw_not_found()
        {
            await Assert.ThrowsAsync<NotFoundException>(() => WithAsync<BrandDto, IBrandService>(s => s.GetDetailAsync(999999)));
            await Assert.ThrowsAsync<NotFoundException>(() => WithAsync<IBrandService>(s => s.DeleteAsync(999999)));
            await Assert.ThrowsAsync<NotFoundException>(() => WithAsync<IBrandService>(s => s.UpdateAsync(999999, new UpdateBrandDto { Name = "x" })));
        }

        [Fact]
        public async Task Brand_lists_are_oldest_first_for_admin_and_newest_first_for_the_site()
        {
            await WithAsync<IBrandService>(s => s.CreateAsync(new CreateBrandDto { Name = "OrderA", Image = Image() }));
            await Task.Delay(30);
            await WithAsync<IBrandService>(s => s.CreateAsync(new CreateBrandDto { Name = "OrderB", Image = Image() }));

            var admin = (await WithAsync<IEnumerable<BrandDto>, IBrandService>(s => s.GetAllAsync())).Select(b => b.Name).ToList();
            var site = (await WithAsync<IEnumerable<BrandDto>, IBrandService>(s => s.GetAllUIAsync())).Select(b => b.Name).ToList();

            Assert.True(admin.IndexOf("OrderA") < admin.IndexOf("OrderB"));
            Assert.True(site.IndexOf("OrderB") < site.IndexOf("OrderA"));
        }

        // ---------- Kart dizaynları ----------

        [Fact]
        public async Task Card_designs_seed_has_three_designs_on_home_in_display_order()
        {
            var home = (await WithAsync<IEnumerable<CardDesignDto>, ICardDesignService>(s => s.GetHomeAsync())).ToList();

            Assert.Equal(3, home.Count);
            Assert.Equal(new[] { "Regular", "Silver", "Gold" }, home.Select(d => d.Title).ToArray());
        }

        [Fact]
        public async Task Only_three_designs_can_be_shown_on_home()
        {
            var error = await Assert.ThrowsAsync<BadRequestException>(() => WithAsync<ICardDesignService>(s => s.CreateAsync(new CreateCardDesignDto { Title = "Fourth", ShowOnHome = true, DisplayOrder = 9, Image = Image() })));

            Assert.Equal("Only 3 designs can be shown on Home. Hide another one first.", error.Message);
        }

        [Fact]
        public async Task A_hidden_design_can_be_created_updated_and_deleted_when_unused()
        {
            await WithAsync<ICardDesignService>(s => s.CreateAsync(new CreateCardDesignDto { Title = "  Hidden  ", ShowOnHome = false, DisplayOrder = 20, Image = Image() }));
            var design = (await WithAsync<IEnumerable<CardDesignAdminDto>, ICardDesignService>(s => s.GetAllAsync())).Single(d => d.Title == "Hidden");
            Assert.Matches(@"^/images/cards/[0-9a-f]{32}\.png$", design.Image);

            await WithAsync<ICardDesignService>(s => s.UpdateAsync(design.Id, new UpdateCardDesignDto { Title = "Hidden 2", ShowOnHome = false, DisplayOrder = 21, Image = Image() }));
            var updated = await WithAsync<CardDesignAdminDto, ICardDesignService>(s => s.GetDetailAsync(design.Id));
            Assert.Equal("Hidden 2", updated.Title);
            Assert.Equal(21, updated.DisplayOrder);
            Assert.False(File.Exists(PathOnDisk(design.Image)));
            Assert.True(File.Exists(PathOnDisk(updated.Image)));

            await WithAsync<ICardDesignService>(s => s.DeleteAsync(design.Id));
            await Assert.ThrowsAsync<NotFoundException>(() => WithAsync<CardDesignAdminDto, ICardDesignService>(s => s.GetDetailAsync(design.Id)));
            Assert.False(File.Exists(PathOnDisk(updated.Image)));
        }

        [Fact]
        public async Task A_design_used_by_a_card_type_cannot_be_deleted()
        {
            var designs = await WithAsync<IEnumerable<CardDesignAdminDto>, ICardDesignService>(s => s.GetAllAsync());
            var regular = designs.Single(d => d.Title == "Regular");

            var error = await Assert.ThrowsAsync<BadRequestException>(() => WithAsync<ICardDesignService>(s => s.DeleteAsync(regular.Id)));

            Assert.StartsWith("This design is used by a card type", error.Message);
        }

        [Fact]
        public async Task Showing_a_design_on_home_again_respects_the_limit_but_not_for_itself()
        {
            var designs = (await WithAsync<IEnumerable<CardDesignAdminDto>, ICardDesignService>(s => s.GetAllAsync())).ToList();
            var silver = designs.Single(d => d.Title == "Silver");
            var cashback = designs.Single(d => d.Title == "Cashback");

            // Silver özü üçün sayılmır: dəyişmədən saxlana bilər
            await WithAsync<ICardDesignService>(s => s.UpdateAsync(silver.Id, new UpdateCardDesignDto { Title = "Silver", ShowOnHome = true, DisplayOrder = silver.DisplayOrder }));
            // Cashback-i də göstərmək dördüncü olardı
            await Assert.ThrowsAsync<BadRequestException>(() => WithAsync<ICardDesignService>(s => s.UpdateAsync(cashback.Id, new UpdateCardDesignDto { Title = "Cashback", ShowOnHome = true, DisplayOrder = cashback.DisplayOrder })));
        }

        [Fact]
        public async Task Card_design_actions_on_an_unknown_id_throw_not_found()
        {
            await Assert.ThrowsAsync<NotFoundException>(() => WithAsync<CardDesignAdminDto, ICardDesignService>(s => s.GetDetailAsync(999999)));
            await Assert.ThrowsAsync<NotFoundException>(() => WithAsync<ICardDesignService>(s => s.DeleteAsync(999999)));
            await Assert.ThrowsAsync<NotFoundException>(() => WithAsync<ICardDesignService>(s => s.UpdateAsync(999999, new UpdateCardDesignDto { Title = "x", DisplayOrder = 1 })));
        }

        // ---------- Kart növləri ----------

        [Fact]
        public async Task Card_tiers_list_all_four_with_their_rules()
        {
            var tiers = (await WithAsync<IEnumerable<CardTierDto>, ICardTierService>(s => s.GetAllAsync())).ToList();
            var admin = (await WithAsync<IEnumerable<CardTierAdminDto>, ICardTierService>(s => s.GetAllAdminAsync())).ToList();

            Assert.Equal(4, tiers.Count);
            Assert.Equal(4, admin.Count);
            var standard = tiers.Single(t => t.Tier == "Standard");
            Assert.Equal(10m, standard.IssueFee);
            Assert.Equal(500m, standard.TransferLimit);
            Assert.False(string.IsNullOrEmpty(standard.DesignImage));
        }

        [Fact]
        public async Task Card_tier_update_changes_the_rules_and_the_design()
        {
            var admin = (await WithAsync<IEnumerable<CardTierAdminDto>, ICardTierService>(s => s.GetAllAdminAsync())).ToList();
            var silver = admin.Single(t => t.Tier == "Silver");
            var gold = admin.Single(t => t.Tier == "Gold");

            await WithAsync<ICardTierService>(s => s.UpdateAsync(silver.Id, new CardTierUpdateDto { IssueFee = 18m, CashbackPercent = 1.25m, TransferLimit = 2500m, CommissionPercent = 0.5m, CardDesignId = gold.CardDesignId }));

            var after = await WithAsync<CardTierAdminDto, ICardTierService>(s => s.GetDetailAsync(silver.Id));
            Assert.Equal(18m, after.IssueFee);
            Assert.Equal(1.25m, after.CashbackPercent);
            Assert.Equal(2500m, after.TransferLimit);
            Assert.Equal(0.5m, after.CommissionPercent);
            Assert.Equal(gold.CardDesignId, after.CardDesignId);
            Assert.Equal("Gold", after.DesignTitle);
        }

        [Fact]
        public async Task Card_tier_update_needs_an_existing_design_and_valid_numbers()
        {
            var silver = (await WithAsync<IEnumerable<CardTierAdminDto>, ICardTierService>(s => s.GetAllAdminAsync())).Single(t => t.Tier == "Gold");

            var noDesign = await Assert.ThrowsAsync<BadRequestException>(() => WithAsync<ICardTierService>(s => s.UpdateAsync(silver.Id, new CardTierUpdateDto { IssueFee = 1m, CashbackPercent = 1m, TransferLimit = 1m, CommissionPercent = 1m, CardDesignId = 999999 })));
            await Assert.ThrowsAsync<BadRequestException>(() => WithAsync<ICardTierService>(s => s.UpdateAsync(silver.Id, new CardTierUpdateDto { IssueFee = -1m, CashbackPercent = 1m, TransferLimit = 1m, CommissionPercent = 1m, CardDesignId = silver.CardDesignId })));
            await Assert.ThrowsAsync<NotFoundException>(() => WithAsync<ICardTierService>(s => s.UpdateAsync(999999, new CardTierUpdateDto { IssueFee = 1m, CashbackPercent = 1m, TransferLimit = 1m, CommissionPercent = 1m, CardDesignId = silver.CardDesignId })));

            Assert.Equal("Choose an existing card design.", noDesign.Message);
            await Assert.ThrowsAsync<NotFoundException>(() => WithAsync<CardTierAdminDto, ICardTierService>(s => s.GetDetailAsync(999999)));
        }

        // ---------- About ----------

        [Fact]
        public async Task About_text_is_updated_trimmed_and_shown_on_the_site()
        {
            var about = await EnsureAboutAsync();

            await WithAsync<IAboutService>(s => s.UpdateAsync(about.Id, new AboutUpdateDto { Label = " Label ", Title = " Title ", Description = " Text " }));

            var ui = await WithAsync<AboutDto?, IAboutService>(s => s.GetUIAsync());
            Assert.Equal("Label", ui!.Label);
            Assert.Equal("Title", ui.Title);
            Assert.Equal("Text", ui.Description);
            Assert.Equal(about.Id, (await WithAsync<AboutDto, IAboutService>(s => s.GetAsync())).Id);
        }

        [Fact]
        public async Task About_video_is_replaced_and_the_old_uploaded_video_is_removed()
        {
            var about = await EnsureAboutAsync();
            await WithAsync<IAboutService>(s => s.UpdateVideoAsync(about.Id, new UpdateAboutVideoDto { Video = FileServiceTests.Make(new byte[] { 0, 0, 0, 0x18, (byte)'f', (byte)'t', (byte)'y', (byte)'p', 1, 2, 3, 4 }) }));
            var firstPath = (await WithAsync<AboutDto, IAboutService>(s => s.GetAsync())).VideoPath;
            Assert.Matches(@"^/videos/about/[0-9a-f]{32}\.mp4$", firstPath);
            Assert.True(File.Exists(PathOnDisk(firstPath)));

            await WithAsync<IAboutService>(s => s.UpdateVideoAsync(about.Id, new UpdateAboutVideoDto { Video = FileServiceTests.Make(new byte[] { 0x1A, 0x45, 0xDF, 0xA3, 1, 2, 3, 4, 5, 6, 7, 8 }) }));

            var secondPath = (await WithAsync<AboutDto, IAboutService>(s => s.GetAsync())).VideoPath;
            Assert.EndsWith(".webm", secondPath);
            Assert.False(File.Exists(PathOnDisk(firstPath)));
            Assert.True(File.Exists(PathOnDisk(secondPath)));
        }

        [Fact]
        public async Task About_video_must_be_a_real_video()
        {
            var about = await EnsureAboutAsync();

            await Assert.ThrowsAsync<BadRequestException>(() => WithAsync<IAboutService>(s => s.UpdateVideoAsync(about.Id, new UpdateAboutVideoDto { Video = null })));
            await Assert.ThrowsAsync<BadRequestException>(() => WithAsync<IAboutService>(s => s.UpdateVideoAsync(about.Id, new UpdateAboutVideoDto { Video = Image() })));
            await Assert.ThrowsAsync<NotFoundException>(() => WithAsync<IAboutService>(s => s.UpdateVideoAsync(999999, new UpdateAboutVideoDto { Video = FileServiceTests.Make(new byte[] { 0, 0, 0, 0x18, (byte)'f', (byte)'t', (byte)'y', (byte)'p', 1, 2, 3, 4 }) })));
        }

        [Fact]
        public async Task About_update_validates_and_reports_unknown_ids()
        {
            await Assert.ThrowsAsync<BadRequestException>(() => WithAsync<IAboutService>(s => s.UpdateAsync(1, new AboutUpdateDto { Label = "", Title = "t", Description = "d" })));
            await Assert.ThrowsAsync<NotFoundException>(() => WithAsync<IAboutService>(s => s.UpdateAsync(999999, new AboutUpdateDto { Label = "l", Title = "t", Description = "d" })));
        }

        // ---------- About sütunları ----------

        [Fact]
        public async Task About_pillars_are_created_under_the_about_block_and_listed()
        {
            var about = await EnsureAboutAsync();

            await WithAsync<IAboutPillarService>(s => s.CreateAsync(new CreateAboutPillarDto { Title = " Pillar ", Description = " Strong ", Image = Image() }));

            var pillar = (await WithAsync<IEnumerable<AboutPillarDto>, IAboutPillarService>(s => s.GetAllAsync())).Single(p => p.Title == "Pillar");
            Assert.Equal("Strong", pillar.Description);
            Assert.Matches(@"^/images/pillars/[0-9a-f]{32}\.png$", pillar.Image);
            var stored = await _app.QueryAsync(db => db.AboutPillars.AsNoTracking().FirstAsync(p => p.Id == pillar.Id));
            Assert.Equal(about.Id, stored.AboutId);
            Assert.Contains(await WithAsync<IEnumerable<AboutPillarDto>, IAboutPillarService>(s => s.GetAllUIAsync()), p => p.Id == pillar.Id);
        }

        [Fact]
        public async Task About_pillar_can_be_updated_with_and_without_a_new_image_and_deleted()
        {
            await EnsureAboutAsync();
            await WithAsync<IAboutPillarService>(s => s.CreateAsync(new CreateAboutPillarDto { Title = "Edit me", Description = "d", Image = Image() }));
            var pillar = (await WithAsync<IEnumerable<AboutPillarDto>, IAboutPillarService>(s => s.GetAllAsync())).Single(p => p.Title == "Edit me");

            await WithAsync<IAboutPillarService>(s => s.UpdateAsync(pillar.Id, new UpdateAboutPillarDto { Title = "Edited", Description = "new" }));
            var textOnly = await WithAsync<AboutPillarDto, IAboutPillarService>(s => s.GetDetailAsync(pillar.Id));
            Assert.Equal("Edited", textOnly.Title);
            Assert.Equal(pillar.Image, textOnly.Image);

            await WithAsync<IAboutPillarService>(s => s.UpdateAsync(pillar.Id, new UpdateAboutPillarDto { Title = "Edited", Description = "new", Image = Image() }));
            var withImage = await WithAsync<AboutPillarDto, IAboutPillarService>(s => s.GetDetailAsync(pillar.Id));
            Assert.NotEqual(pillar.Image, withImage.Image);
            Assert.False(File.Exists(PathOnDisk(pillar.Image)));

            await WithAsync<IAboutPillarService>(s => s.DeleteAsync(pillar.Id));
            Assert.False(File.Exists(PathOnDisk(withImage.Image)));
            await Assert.ThrowsAsync<NotFoundException>(() => WithAsync<AboutPillarDto, IAboutPillarService>(s => s.GetDetailAsync(pillar.Id)));
        }

        [Fact]
        public async Task About_pillar_needs_an_image_and_a_description()
        {
            await EnsureAboutAsync();
            await Assert.ThrowsAsync<BadRequestException>(() => WithAsync<IAboutPillarService>(s => s.CreateAsync(new CreateAboutPillarDto { Title = "t", Description = "d", Image = null })));
            await Assert.ThrowsAsync<BadRequestException>(() => WithAsync<IAboutPillarService>(s => s.CreateAsync(new CreateAboutPillarDto { Title = "t", Description = "", Image = Image() })));
            await Assert.ThrowsAsync<NotFoundException>(() => WithAsync<IAboutPillarService>(s => s.DeleteAsync(999999)));
        }

        // ---------- Xidmətlər ----------

        [Fact]
        public async Task Service_section_and_items_can_be_edited()
        {
            var (section, item) = await _app.QueryAsync(async db =>
            {
                var s = await db.ServiceSections.FirstOrDefaultAsync();
                if (s is null)
                {
                    s = new ServiceSection { Label = "Services", Title = "Title", Description = "Desc" };
                    db.ServiceSections.Add(s);
                    await db.SaveChangesAsync();
                }
                var i = new ServiceItem { Number = 900 + (int)(TestData.Next() % 90), Icon = "/images/services/old.png", Title = "Cards", Description = "Old", ServiceSectionId = s.Id };
                db.ServiceItems.Add(i);
                await db.SaveChangesAsync();
                return (s, i);
            });

            await WithAsync<IServiceSectionService>(s => s.UpdateAsync(section.Id, new ServiceSectionUpdateDto { Label = " L ", Title = " T ", Description = " D " }));
            var ui = await WithAsync<ServiceSectionDto?, IServiceSectionService>(s => s.GetUIAsync());
            Assert.Equal("L", ui!.Label);
            Assert.Equal("T", ui.Title);
            Assert.Equal("D", ui.Description);

            await WithAsync<IServiceItemService>(s => s.UpdateAsync(item.Id, new ServiceItemUpdateDto { Title = " New ", Description = " Text " }));
            var kept = await WithAsync<ServiceItemDto, IServiceItemService>(s => s.GetDetailAsync(item.Id));
            Assert.Equal("New", kept.Title);
            Assert.Equal("/images/services/old.png", kept.Icon);          // yeni ikon seçilməyib

            await WithAsync<IServiceItemService>(s => s.UpdateAsync(item.Id, new ServiceItemUpdateDto { Title = "New", Description = "Text", Icon = Image() }));
            var changed = await WithAsync<ServiceItemDto, IServiceItemService>(s => s.GetDetailAsync(item.Id));
            Assert.Matches(@"^/images/services/[0-9a-f]{32}\.png$", changed.Icon);
            Assert.Contains(await WithAsync<IEnumerable<ServiceItemDto>, IServiceItemService>(s => s.GetAllUIAsync()), x => x.Id == item.Id);
        }

        [Fact]
        public async Task Service_items_are_ordered_by_number()
        {
            var items = (await WithAsync<IEnumerable<ServiceItemDto>, IServiceItemService>(s => s.GetAllAsync())).ToList();
            Assert.Equal(items.OrderBy(i => i.Number).Select(i => i.Id), items.Select(i => i.Id));
        }

        [Fact]
        public async Task Service_updates_validate_and_report_unknown_ids()
        {
            await Assert.ThrowsAsync<BadRequestException>(() => WithAsync<IServiceItemService>(s => s.UpdateAsync(1, new ServiceItemUpdateDto { Title = "", Description = "d" })));
            await Assert.ThrowsAsync<NotFoundException>(() => WithAsync<IServiceItemService>(s => s.UpdateAsync(999999, new ServiceItemUpdateDto { Title = "t", Description = "d" })));
            await Assert.ThrowsAsync<NotFoundException>(() => WithAsync<ServiceItemDto, IServiceItemService>(s => s.GetDetailAsync(999999)));
            await Assert.ThrowsAsync<NotFoundException>(() => WithAsync<IServiceSectionService>(s => s.UpdateAsync(999999, new ServiceSectionUpdateDto { Label = "l", Title = "t", Description = "d" })));
        }

        // ---------- Üstünlüklər ----------

        private async Task<BenefitSection> EnsureBenefitSectionAsync()
        {
            return await _app.QueryAsync(async db =>
            {
                var section = await db.BenefitSections.FirstOrDefaultAsync();
                if (section is null)
                {
                    section = new BenefitSection { Label = "Benefits", Title = "Title", Description = "Desc" };
                    db.BenefitSections.Add(section);
                    await db.SaveChangesAsync();
                }
                return section;
            });
        }

        private static BenefitItemCreateDto Benefit(string title = "Travel", string url = "/App")
        {
            return new BenefitItemCreateDto { Label = "CARDS", Title = title, Description = "Desc", ButtonText = "Go", ButtonUrl = url, Text1 = "one", Text2 = "two", Text3 = "three" };
        }

        [Fact]
        public async Task Benefit_section_can_be_edited_and_read_for_the_site()
        {
            var section = await EnsureBenefitSectionAsync();

            await WithAsync<IBenefitSectionService>(s => s.UpdateAsync(section.Id, new BenefitSectionUpdateDto { Label = " L ", Title = " T ", Description = " D " }));

            var ui = await WithAsync<BenefitSectionDto?, IBenefitSectionService>(s => s.GetUIAsync());
            Assert.Equal("L", ui!.Label);
            Assert.Equal("D", ui.Description);
            Assert.Equal(section.Id, (await WithAsync<BenefitSectionDto, IBenefitSectionService>(s => s.GetAsync())).Id);
            await Assert.ThrowsAsync<NotFoundException>(() => WithAsync<IBenefitSectionService>(s => s.UpdateAsync(999999, new BenefitSectionUpdateDto { Label = "l", Title = "t", Description = "d" })));
        }

        [Fact]
        public async Task Benefit_items_can_be_created_updated_and_deleted_and_the_button_is_limited_to_known_pages()
        {
            var section = await EnsureBenefitSectionAsync();

            await WithAsync<IBenefitItemService>(s => s.CreateAsync(Benefit("  First benefit  ", "/App/Transfer")));
            var item = (await WithAsync<IEnumerable<BenefitItemDto>, IBenefitItemService>(s => s.GetAllAsync())).Single(b => b.Title == "First benefit");
            Assert.Equal("/App/Transfer", item.ButtonUrl);
            Assert.Equal(section.Id, item.BenefitSectionId);

            var update = new BenefitItemUpdateDto { Label = "L", Title = "Changed", Description = "D", ButtonText = "B", ButtonUrl = "/App", Text1 = "a", Text2 = "b", Text3 = "c" };
            await WithAsync<IBenefitItemService>(s => s.UpdateAsync(item.Id, update));
            Assert.Equal("Changed", (await WithAsync<BenefitItemDto, IBenefitItemService>(s => s.GetDetailAsync(item.Id))).Title);

            await Assert.ThrowsAsync<BadRequestException>(() => WithAsync<IBenefitItemService>(s => s.CreateAsync(Benefit("Bad link", "https://evil.example"))));
            await Assert.ThrowsAsync<BadRequestException>(() => WithAsync<IBenefitItemService>(s => s.UpdateAsync(item.Id, new BenefitItemUpdateDto { Label = "L", Title = "T", Description = "D", ButtonText = "B", ButtonUrl = "javascript:1", Text1 = "a", Text2 = "b", Text3 = "c" })));

            await WithAsync<IBenefitItemService>(s => s.DeleteAsync(item.Id));
            await Assert.ThrowsAsync<NotFoundException>(() => WithAsync<BenefitItemDto, IBenefitItemService>(s => s.GetDetailAsync(item.Id)));
        }

        [Fact]
        public async Task Benefit_destinations_list_the_allowed_pages()
        {
            using var scope = _app.NewScope();
            var destinations = scope.ServiceProvider.GetRequiredService<IBenefitItemService>().GetDestinations().ToList();

            Assert.Equal(Domain.Constants.ButtonDestinations.All.Length, destinations.Count);
            Assert.Contains(destinations, d => d.Path == "/App");
        }

        [Fact]
        public async Task Benefit_item_list_for_the_site_is_newest_first()
        {
            await EnsureBenefitSectionAsync();
            await WithAsync<IBenefitItemService>(s => s.CreateAsync(Benefit("Site older")));
            await Task.Delay(30);
            await WithAsync<IBenefitItemService>(s => s.CreateAsync(Benefit("Site newer")));

            var titles = (await WithAsync<IEnumerable<BenefitItemDto>, IBenefitItemService>(s => s.GetAllUIAsync())).Select(b => b.Title).ToList();

            Assert.True(titles.IndexOf("Site newer") < titles.IndexOf("Site older"));
        }

        [Fact]
        public async Task Benefit_item_actions_on_an_unknown_id_throw_not_found()
        {
            await Assert.ThrowsAsync<NotFoundException>(() => WithAsync<IBenefitItemService>(s => s.DeleteAsync(999999)));
            await Assert.ThrowsAsync<NotFoundException>(() => WithAsync<IBenefitItemService>(s => s.UpdateAsync(999999, new BenefitItemUpdateDto { Label = "L", Title = "T", Description = "D", ButtonText = "B", ButtonUrl = "/App", Text1 = "a", Text2 = "b", Text3 = "c" })));
        }

        // ---------- Ticker və hero ----------

        [Fact]
        public async Task Ticker_items_can_be_created_updated_and_deleted()
        {
            await WithAsync<IHomeTickerService>(s => s.CreateAsync(new CreateHomeTickerDto { Text = "  Open 24/7  " }));
            var ticker = (await WithAsync<IEnumerable<HomeTickerDto>, IHomeTickerService>(s => s.GetAllAsync())).Single(t => t.Text == "Open 24/7");

            await WithAsync<IHomeTickerService>(s => s.UpdateAsync(ticker.Id, new UpdateHomeTickerDto { Text = " Always open " }));
            Assert.Equal("Always open", (await WithAsync<HomeTickerDto, IHomeTickerService>(s => s.GetDetailAsync(ticker.Id))).Text);
            Assert.Contains(await WithAsync<IEnumerable<HomeTickerDto>, IHomeTickerService>(s => s.GetAllUIAsync()), t => t.Id == ticker.Id);

            await WithAsync<IHomeTickerService>(s => s.DeleteAsync(ticker.Id));
            await Assert.ThrowsAsync<NotFoundException>(() => WithAsync<HomeTickerDto, IHomeTickerService>(s => s.GetDetailAsync(ticker.Id)));
        }

        [Fact]
        public async Task Ticker_validates_and_reports_unknown_ids()
        {
            await Assert.ThrowsAsync<BadRequestException>(() => WithAsync<IHomeTickerService>(s => s.CreateAsync(new CreateHomeTickerDto { Text = "" })));
            await Assert.ThrowsAsync<BadRequestException>(() => WithAsync<IHomeTickerService>(s => s.CreateAsync(new CreateHomeTickerDto { Text = new string('a', 101) })));
            await Assert.ThrowsAsync<NotFoundException>(() => WithAsync<IHomeTickerService>(s => s.UpdateAsync(999999, new UpdateHomeTickerDto { Text = "x" })));
            await Assert.ThrowsAsync<NotFoundException>(() => WithAsync<IHomeTickerService>(s => s.DeleteAsync(999999)));
        }

        [Fact]
        public async Task Card_hero_text_is_updated_and_shown_on_the_site()
        {
            var hero = await _app.QueryAsync(async db =>
            {
                var existing = await db.CardHeroes.FirstOrDefaultAsync();
                if (existing is null)
                {
                    existing = new CardHero { Label = "CARDS", Title = "Title", Description = "Desc" };
                    db.CardHeroes.Add(existing);
                    await db.SaveChangesAsync();
                }
                return existing;
            });

            await WithAsync<ICardHeroService>(s => s.UpdateAsync(hero.Id, new UpdateCardHeroDto { Label = " L ", Title = " T ", Description = " D " }));

            var ui = await WithAsync<CardHeroDto?, ICardHeroService>(s => s.GetUIAsync());
            Assert.Equal("L", ui!.Label);
            Assert.Equal("T", ui.Title);
            Assert.Equal("D", ui.Description);
            await Assert.ThrowsAsync<BadRequestException>(() => WithAsync<ICardHeroService>(s => s.UpdateAsync(hero.Id, new UpdateCardHeroDto { Label = "", Title = "t", Description = "d" })));
            await Assert.ThrowsAsync<NotFoundException>(() => WithAsync<ICardHeroService>(s => s.UpdateAsync(999999, new UpdateCardHeroDto { Label = "l", Title = "t", Description = "d" })));
        }

        // ---------- Tənzimləmələr ----------

        private async Task<Setting> SettingAsync(string key, string value)
        {
            return await _app.QueryAsync(async db =>
            {
                var existing = await db.Settings.FirstOrDefaultAsync(s => s.Key == key);
                if (existing is null)
                {
                    existing = new Setting { Key = key, Value = value };
                    db.Settings.Add(existing);
                    await db.SaveChangesAsync();
                }
                return existing;
            });
        }

        [Fact]
        public async Task Settings_are_listed_for_admin_and_as_a_dictionary_for_the_site()
        {
            var company = await SettingAsync("CompanyName", "Caspian Bank");

            var list = await WithAsync<List<SettingDto>, ISettingService>(s => s.GetAllAsync());
            var dictionary = await WithAsync<Dictionary<string, string>, ISettingService>(s => s.GetAllUIAsync());

            Assert.Contains(list, s => s.Key == "CompanyName");
            Assert.Equal(company.Value, dictionary["CompanyName"]);
            Assert.Equal(list.OrderBy(s => s.Id).Select(s => s.Id), list.Select(s => s.Id));
            Assert.Equal(company.Key, (await WithAsync<SettingDto, ISettingService>(s => s.GetByIdAsync(company.Id))).Key);
        }

        [Fact]
        public async Task A_setting_value_is_trimmed_and_validated_by_its_key()
        {
            var address = await SettingAsync("Address", "28 Nizami Street, Baku");
            var email = await SettingAsync("Email", "a@b.az");

            await WithAsync<ISettingService>(s => s.UpdateAsync(address.Id, new UpdateSettingDto { Value = "  1 New Street  " }));
            Assert.Equal("1 New Street", (await WithAsync<SettingDto, ISettingService>(s => s.GetByIdAsync(address.Id))).Value);

            await Assert.ThrowsAsync<BadRequestException>(() => WithAsync<ISettingService>(s => s.UpdateAsync(address.Id, new UpdateSettingDto { Value = new string('x', 201) })));
            await Assert.ThrowsAsync<BadRequestException>(() => WithAsync<ISettingService>(s => s.UpdateAsync(email.Id, new UpdateSettingDto { Value = "not-an-email" })));
            await Assert.ThrowsAsync<NotFoundException>(() => WithAsync<ISettingService>(s => s.UpdateAsync(999999, new UpdateSettingDto { Value = "x" })));
            await Assert.ThrowsAsync<NotFoundException>(() => WithAsync<SettingDto, ISettingService>(s => s.GetByIdAsync(999999)));
        }

        [Fact]
        public async Task The_request_cannot_change_the_setting_key_that_decides_the_validation()
        {
            var phone = await SettingAsync("PhoneNumber", "+994 12 345 67 89");

            // Müştərinin göndərdiyi Key qaydaları dəyişə bilməz: servis açarı bazadan yazır
            await Assert.ThrowsAsync<BadRequestException>(() => WithAsync<ISettingService>(s => s.UpdateAsync(phone.Id, new UpdateSettingDto { Key = "Address", Value = "not a phone" })));
        }

        [Fact]
        public async Task The_logo_is_changed_by_upload_not_by_text()
        {
            var logo = await SettingAsync("Logo", "/images/logo.png");

            var error = await Assert.ThrowsAsync<BadRequestException>(() => WithAsync<ISettingService>(s => s.UpdateAsync(logo.Id, new UpdateSettingDto { Value = "/images/other.png" })));

            Assert.Equal("The logo is changed by uploading an image.", error.Message);
            Assert.Equal(logo.Value, (await WithAsync<SettingDto, ISettingService>(s => s.GetByIdAsync(logo.Id))).Value);
        }

        [Fact]
        public async Task Logo_upload_replaces_the_value_and_keeps_the_seed_file()
        {
            var logo = await SettingAsync("Logo", "/images/logo.png");

            await WithAsync<ISettingService>(s => s.UpdateLogoAsync(logo.Id, new UpdateSettingLogoDto { Image = Image() }));
            var first = (await WithAsync<SettingDto, ISettingService>(s => s.GetByIdAsync(logo.Id))).Value;
            Assert.Matches(@"^/images/settings/[0-9a-f]{32}\.png$", first);

            await WithAsync<ISettingService>(s => s.UpdateLogoAsync(logo.Id, new UpdateSettingLogoDto { Image = Image() }));
            var second = (await WithAsync<SettingDto, ISettingService>(s => s.GetByIdAsync(logo.Id))).Value;
            Assert.NotEqual(first, second);
            Assert.False(File.Exists(PathOnDisk(first)));
            Assert.True(File.Exists(PathOnDisk(second)));
        }

        [Fact]
        public async Task Logo_upload_only_works_for_the_logo_setting_and_needs_an_image()
        {
            var logo = await SettingAsync("Logo", "/images/logo.png");
            var address = await SettingAsync("Address", "28 Nizami Street, Baku");

            var notLogo = await Assert.ThrowsAsync<BadRequestException>(() => WithAsync<ISettingService>(s => s.UpdateLogoAsync(address.Id, new UpdateSettingLogoDto { Image = Image() })));
            await Assert.ThrowsAsync<BadRequestException>(() => WithAsync<ISettingService>(s => s.UpdateLogoAsync(logo.Id, new UpdateSettingLogoDto { Image = null })));
            await Assert.ThrowsAsync<NotFoundException>(() => WithAsync<ISettingService>(s => s.UpdateLogoAsync(999999, new UpdateSettingLogoDto { Image = Image() })));

            Assert.Equal("Only the logo is changed by uploading an image.", notLogo.Message);
        }
    }
}
