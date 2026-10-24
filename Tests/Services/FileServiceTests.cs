using CaspianBank.Tests.Infrastructure;
using CaspianBank.Tests.Validators;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Service.Helpers;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;
using Xunit;

namespace CaspianBank.Tests.Services
{
    public class FileServiceTests
    {
        private static readonly byte[] PngHeader = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        public static IFormFile Png(int extra = 10, string name = "pic.png")
        {
            return Make(PngHeader.Concat(new byte[extra]).ToArray(), name);
        }

        public static IFormFile Make(byte[] content, string name = "file.bin")
        {
            return new FormFile(new MemoryStream(content), 0, content.Length, "file", name);
        }

        private static byte[] Jpeg()
        {
            return new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0, 0, 0, 0 };
        }

        private static byte[] Webp()
        {
            return "RIFF\0\0\0\0WEBPVP8 ".Select(c => (byte)c).ToArray();
        }

        private static byte[] Mp4()
        {
            return new byte[] { 0, 0, 0, 0x18, (byte)'f', (byte)'t', (byte)'y', (byte)'p', (byte)'m', (byte)'p', (byte)'4', (byte)'2', 0, 0, 0, 0 };
        }

        private static byte[] WebM()
        {
            return new byte[] { 0x1A, 0x45, 0xDF, 0xA3, 0, 0, 0, 0, 0, 0, 0, 0 };
        }

        private static async Task<(TestApp App, IFileService Files, IServiceScope Scope)> OpenAsync()
        {
            var app = await TestApp.SharedAsync();
            var scope = app.NewScope();
            return (app, scope.ServiceProvider.GetRequiredService<IFileService>(), scope);
        }

        // ---------- Şəkil ----------

        [Theory]
        [InlineData("png")]
        [InlineData("jpg")]
        [InlineData("webp")]
        public async Task Upload_saves_each_image_type_under_a_random_name_with_the_detected_extension(string type)
        {
            var (app, files, scope) = await OpenAsync();
            using var _ = scope;
            var content = type == "png" ? PngHeader.Concat(new byte[10]).ToArray() : type == "jpg" ? Jpeg() : Webp();

            var path = await files.UploadFileAsync(Make(content, "holiday.exe"), "brands");

            Assert.Matches($@"^/images/brands/[0-9a-f]{{32}}\.{type}$", path);
            var full = Path.Combine(app.WebRoot, path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(full));
            Assert.Equal(content, await File.ReadAllBytesAsync(full));
        }

        [Fact]
        public async Task Upload_gives_a_different_name_every_time()
        {
            var (_, files, scope) = await OpenAsync();
            using var __ = scope;
            var first = await files.UploadFileAsync(Png(), "brands");
            var second = await files.UploadFileAsync(Png(), "brands");
            Assert.NotEqual(first, second);
        }

        [Fact]
        public async Task Upload_rejects_a_file_that_is_not_an_image_even_with_a_png_name()
        {
            var (_, files, scope) = await OpenAsync();
            using var __ = scope;

            var error = await Assert.ThrowsAsync<BadRequestException>(() => files.UploadFileAsync(Make(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, "fake.png"), "brands"));

            Assert.Equal("Only PNG, JPEG or WebP images are allowed.", error.Message);
        }

        [Fact]
        public async Task Upload_rejects_an_empty_file_and_a_file_over_two_megabytes()
        {
            var (_, files, scope) = await OpenAsync();
            using var __ = scope;

            var empty = await Assert.ThrowsAsync<BadRequestException>(() => files.UploadFileAsync(V.EmptyFile(), "brands"));
            var big = await Assert.ThrowsAsync<BadRequestException>(() => files.UploadFileAsync(Png(ImageFileRules.MaxBytes), "brands"));

            Assert.Equal("Choose an image.", empty.Message);
            Assert.Equal("The image must be up to 2 MB.", big.Message);
        }

        [Fact]
        public async Task Upload_accepts_a_file_of_exactly_two_megabytes()
        {
            var (_, files, scope) = await OpenAsync();
            using var __ = scope;
            var path = await files.UploadFileAsync(Png(ImageFileRules.MaxBytes - PngHeader.Length), "brands");
            Assert.EndsWith(".png", path);
        }

        [Theory]
        [InlineData("png")]
        [InlineData("jpg")]
        [InlineData("webp")]
        [InlineData("none")]
        public void DetectExtension_reads_the_signature_not_the_name(string type)
        {
            byte[] content = type switch
            {
                "png" => PngHeader.Concat(new byte[4]).ToArray(),
                "jpg" => Jpeg(),
                "webp" => Webp(),
                _ => new byte[] { 0, 1, 2 }
            };
            var expected = type == "none" ? null : "." + type;
            Assert.Equal(expected, ImageFileRules.DetectExtension(content));
        }

        [Fact]
        public void DetectExtension_needs_the_whole_signature()
        {
            Assert.Null(ImageFileRules.DetectExtension(Array.Empty<byte>()));
            Assert.Null(ImageFileRules.DetectExtension(new byte[] { 0x89, 0x50, 0x4E }));
            Assert.Null(ImageFileRules.DetectExtension(new byte[] { 0xFF, 0xD8 }));
            Assert.Null(ImageFileRules.DetectExtension(PngHeader.Take(7).ToArray()));
        }

        // ---------- Video ----------

        [Theory]
        [InlineData("mp4")]
        [InlineData("webm")]
        public async Task UploadVideo_saves_mp4_and_webm(string type)
        {
            var (app, files, scope) = await OpenAsync();
            using var __ = scope;
            var content = type == "mp4" ? Mp4() : WebM();

            var path = await files.UploadVideoAsync(Make(content, "clip.mov"), "about");

            Assert.Matches($@"^/videos/about/[0-9a-f]{{32}}\.{type}$", path);
            var full = Path.Combine(app.WebRoot, path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            Assert.Equal(content, await File.ReadAllBytesAsync(full));
        }

        [Fact]
        public async Task UploadVideo_keeps_the_whole_content_of_a_larger_file()
        {
            var (app, files, scope) = await OpenAsync();
            using var __ = scope;
            var content = Mp4().Concat(Enumerable.Range(0, 100000).Select(i => (byte)(i % 251))).ToArray();

            var path = await files.UploadVideoAsync(Make(content), "about");

            var full = Path.Combine(app.WebRoot, path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            Assert.Equal(content, await File.ReadAllBytesAsync(full));
        }

        [Fact]
        public async Task UploadVideo_rejects_other_types_and_empty_files()
        {
            var (_, files, scope) = await OpenAsync();
            using var __ = scope;

            var wrong = await Assert.ThrowsAsync<BadRequestException>(() => files.UploadVideoAsync(Make(Jpeg()), "about"));
            var empty = await Assert.ThrowsAsync<BadRequestException>(() => files.UploadVideoAsync(V.EmptyFile(), "about"));

            Assert.Equal("Only MP4 or WebM videos are allowed.", wrong.Message);
            Assert.Equal("Choose a video.", empty.Message);
        }

        [Fact]
        public void VideoFileRules_detect_by_signature()
        {
            Assert.Equal(".mp4", VideoFileRules.DetectExtension(Mp4()));
            Assert.Equal(".webm", VideoFileRules.DetectExtension(WebM()));
            Assert.Null(VideoFileRules.DetectExtension(new byte[] { 1, 2, 3 }));
            Assert.Null(VideoFileRules.DetectExtension(Array.Empty<byte>()));
        }

        // ---------- Silmə ----------

        [Fact]
        public async Task Delete_removes_a_file_the_system_uploaded()
        {
            var (app, files, scope) = await OpenAsync();
            using var __ = scope;
            var path = await files.UploadFileAsync(Png(), "brands");
            var full = Path.Combine(app.WebRoot, path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(full));

            await files.DeleteFileAsync(path);

            Assert.False(File.Exists(full));
        }

        [Fact]
        public async Task Delete_removes_an_uploaded_video()
        {
            var (app, files, scope) = await OpenAsync();
            using var __ = scope;
            var path = await files.UploadVideoAsync(Make(Mp4()), "about");
            var full = Path.Combine(app.WebRoot, path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

            await files.DeleteFileAsync(path);

            Assert.False(File.Exists(full));
        }

        [Theory]
        [InlineData("/images/brands/regular.png")]                    // başlanğıc fayl: təsadüfi ad deyil
        [InlineData("/images/cards/../../secret/0123456789abcdef0123456789abcdef.png")]
        [InlineData("/images/brands/0123456789ABCDEF0123456789ABCDEF.png")]
        [InlineData("/other/brands/0123456789abcdef0123456789abcdef.png")]
        [InlineData("images/brands/0123456789abcdef0123456789abcdef.png")]
        [InlineData("..\\..\\windows\\system.ini")]
        [InlineData("")]
        [InlineData(null)]
        public async Task Delete_ignores_anything_that_is_not_a_system_uploaded_path(string? path)
        {
            var (_, files, scope) = await OpenAsync();
            using var __ = scope;
            await files.DeleteFileAsync(path!);        // xəta atmamalıdır
        }

        [Fact]
        public async Task Delete_does_not_touch_a_seed_file_with_a_normal_name()
        {
            var (app, files, scope) = await OpenAsync();
            using var __ = scope;
            var directory = Path.Combine(app.WebRoot, "images", "brands");
            Directory.CreateDirectory(directory);
            var seed = Path.Combine(directory, "claude.png");
            await File.WriteAllBytesAsync(seed, new byte[] { 1 });

            await files.DeleteFileAsync("/images/brands/claude.png");

            Assert.True(File.Exists(seed));
        }

        [Fact]
        public async Task Delete_of_a_missing_file_is_silent()
        {
            var (_, files, scope) = await OpenAsync();
            using var __ = scope;
            await files.DeleteFileAsync("/images/brands/0123456789abcdef0123456789abcdef.png");
        }
    }
}
