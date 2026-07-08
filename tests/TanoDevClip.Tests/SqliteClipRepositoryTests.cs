using TanoDevClip.Core.Clipboard;
using TanoDevClip.Infrastructure.Database;

namespace TanoDevClip.Tests
{
    public sealed class SqliteClipRepositoryTests : IDisposable
    {
        private readonly string _databasePath = Path.Combine(
            Path.GetTempPath(),
            $"tanodevclip-tests-{Guid.NewGuid():N}.db");

        [Fact]
        public async Task Should_migrate_existing_clip_table_for_images()
        {
            var connectionFactory = new DatabaseConnectionFactory(_databasePath);
            await using (var connection = connectionFactory.CreateConnection())
            {
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandText = """
                CREATE TABLE clips (
                    id TEXT PRIMARY KEY,
                    content TEXT NOT NULL,
                    content_hash TEXT NOT NULL,
                    clip_type TEXT NOT NULL,
                    title TEXT NULL,
                    source_app TEXT NULL,
                    source_window_title TEXT NULL,
                    source_url TEXT NULL,
                    is_pinned INTEGER NOT NULL DEFAULT 0,
                    created_at TEXT NOT NULL,
                    last_used_at TEXT NULL,
                    use_count INTEGER NOT NULL DEFAULT 0
                );
                """;
                await command.ExecuteNonQueryAsync();
            }

            var bootstrapper = new DatabaseBootstrapper(connectionFactory);
            await bootstrapper.InitializeAsync();

            await using var verifyConnection = connectionFactory.CreateConnection();
            await verifyConnection.OpenAsync();
            await using var verifyCommand = verifyConnection.CreateCommand();
            verifyCommand.CommandText = "PRAGMA table_info(clips);";

            var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await using var reader = await verifyCommand.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                columns.Add(reader.GetString(1));
            }

            Assert.Contains("binary_content", columns);
            Assert.Contains("preview_content", columns);
            Assert.Contains("content_mime_type", columns);
            Assert.Contains("image_width", columns);
            Assert.Contains("image_height", columns);
        }

        [Fact]
        public async Task Should_store_image_preview_in_list_and_full_image_by_id()
        {
            var connectionFactory = new DatabaseConnectionFactory(_databasePath);
            var bootstrapper = new DatabaseBootstrapper(connectionFactory);
            await bootstrapper.InitializeAsync();

            var repository = new SqliteClipRepository(connectionFactory);
            var imageBytes = new byte[] { 137, 80, 78, 71, 1, 2, 3 };
            var previewBytes = new byte[] { 137, 80, 78, 71, 9, 8, 7 };

            await repository.SaveAsync(new ClipItem
            {
                Id = "image-clip",
                Content = "Image 2x2",
                ContentHash = "image-hash",
                ClipType = ClipType.Image,
                BinaryContent = imageBytes,
                PreviewContent = previewBytes,
                ContentMimeType = "image/png",
                ImageWidth = 2,
                ImageHeight = 2,
                Title = "Image 2x2",
                SourceApp = "Tests",
                SourceWindowTitle = "Test Window",
                SourceUrl = null,
                IsPinned = false,
                CreatedAt = DateTimeOffset.UtcNow,
                LastUsedAt = null,
                UseCount = 0
            });

            var listedClip = Assert.Single(await repository.SearchAsync(new ClipSearchFilter
            {
                ClipType = ClipType.Image
            }));

            Assert.Null(listedClip.BinaryContent);
            Assert.Equal(previewBytes, listedClip.PreviewContent);
            Assert.Equal("image/png", listedClip.ContentMimeType);
            Assert.Equal(2, listedClip.ImageWidth);
            Assert.Equal(2, listedClip.ImageHeight);

            var fullClip = await repository.GetByIdAsync("image-clip");

            Assert.NotNull(fullClip);
            Assert.Equal(imageBytes, fullClip.BinaryContent);
            Assert.Equal(previewBytes, fullClip.PreviewContent);
        }

        [Fact]
        public async Task Should_move_existing_clip_to_top_when_same_content_is_copied_again()
        {
            var connectionFactory = new DatabaseConnectionFactory(_databasePath);
            var bootstrapper = new DatabaseBootstrapper(connectionFactory);
            await bootstrapper.InitializeAsync();

            var repository = new SqliteClipRepository(connectionFactory);
            var baseTime = DateTimeOffset.UtcNow;

            await repository.SaveAsync(CreateTextClip("clip-a", "content a", "hash-a", baseTime));
            await repository.SaveAsync(CreateTextClip("clip-b", "content b", "hash-b", baseTime.AddMinutes(1)));
            await repository.SaveAsync(CreateTextClip("clip-a-copy", "content a", "hash-a", baseTime.AddMinutes(2)));

            var clips = await repository.SearchAsync(new ClipSearchFilter());

            Assert.Equal(2, clips.Count);
            Assert.Equal("clip-a", clips[0].Id);
            Assert.Equal("clip-b", clips[1].Id);
            Assert.Equal(baseTime.AddMinutes(2), clips[0].CreatedAt);
        }

        private static ClipItem CreateTextClip(string id, string content, string contentHash, DateTimeOffset createdAt)
        {
            return new ClipItem
            {
                Id = id,
                Content = content,
                ContentHash = contentHash,
                ClipType = ClipType.Text,
                Title = content,
                SourceApp = "Tests",
                SourceWindowTitle = "Test Window",
                SourceUrl = null,
                IsPinned = false,
                CreatedAt = createdAt,
                LastUsedAt = null,
                UseCount = 0
            };
        }

        public void Dispose()
        {
            try
            {
                File.Delete(_databasePath);
            }
            catch
            {
                // Best effort cleanup for Windows file locking during failed tests.
            }
        }
    }
}
