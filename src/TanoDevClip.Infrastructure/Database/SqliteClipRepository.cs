using Microsoft.Data.Sqlite;
using TanoDevClip.Core.Clipboard;
using TanoDevClip.Core.Repositories;

namespace TanoDevClip.Infrastructure.Database
{
    public sealed class SqliteClipRepository : IClipRepository
    {
        private readonly DatabaseConnectionFactory _connectionFactory;

        public SqliteClipRepository(DatabaseConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task SaveAsync(ClipItem clip, CancellationToken cancellationToken = default)
        {
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            // MVP rule: repeated content is ignored by hash. Later this can evolve to
            // update recency or move duplicate handling into a richer domain service.
            await using var command = connection.CreateCommand();
            command.CommandText = """
            INSERT INTO clips (
                id,
                content,
                content_hash,
                clip_type,
                binary_content,
                preview_content,
                content_mime_type,
                image_width,
                image_height,
                title,
                source_app,
                source_window_title,
                source_url,
                is_pinned,
                created_at,
                last_used_at,
                use_count
            )
            SELECT
                $id,
                $content,
                $content_hash,
                $clip_type,
                $binary_content,
                $preview_content,
                $content_mime_type,
                $image_width,
                $image_height,
                $title,
                $source_app,
                $source_window_title,
                $source_url,
                $is_pinned,
                $created_at,
                $last_used_at,
                $use_count
            WHERE NOT EXISTS (
                SELECT 1 FROM clips WHERE content_hash = $content_hash
            );
            """;

            AddClipParameters(command, clip);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<ClipItem>> SearchAsync(
            ClipSearchFilter filter,
            CancellationToken cancellationToken = default)
        {
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            var limit = Math.Clamp(filter.Limit, 1, 500);
            var hasQuery = !string.IsNullOrWhiteSpace(filter.Query);
            var hasType = filter.ClipType is not null;

            await using var command = connection.CreateCommand();
            command.CommandText = $"""
            SELECT
                id,
                content,
                content_hash,
                clip_type,
                NULL AS binary_content,
                preview_content,
                content_mime_type,
                image_width,
                image_height,
                title,
                source_app,
                source_window_title,
                source_url,
                is_pinned,
                created_at,
                last_used_at,
                use_count
            FROM clips
            WHERE (NOT $has_query OR (
                content LIKE $query
                OR title LIKE $query
                OR clip_type LIKE $query
                OR source_app LIKE $query
                OR source_window_title LIKE $query
            ))
            AND (NOT $has_type OR clip_type = $clip_type)
            ORDER BY is_pinned DESC, datetime(created_at) DESC
            LIMIT {limit};
            """;
            command.Parameters.AddWithValue("$has_query", hasQuery ? 1 : 0);
            command.Parameters.AddWithValue("$query", $"%{filter.Query?.Trim()}%");
            command.Parameters.AddWithValue("$has_type", hasType ? 1 : 0);
            command.Parameters.AddWithValue("$clip_type", filter.ClipType?.ToString() ?? string.Empty);

            var clips = new List<ClipItem>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                clips.Add(ReadClip(reader));
            }

            return clips;
        }

        public async Task<ClipItem?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = """
            SELECT
                id,
                content,
                content_hash,
                clip_type,
                binary_content,
                preview_content,
                content_mime_type,
                image_width,
                image_height,
                title,
                source_app,
                source_window_title,
                source_url,
                is_pinned,
                created_at,
                last_used_at,
                use_count
            FROM clips
            WHERE id = $id;
            """;
            command.Parameters.AddWithValue("$id", id);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await reader.ReadAsync(cancellationToken)
                ? ReadClip(reader)
                : null;
        }

        public async Task TogglePinAsync(string id, CancellationToken cancellationToken = default)
        {
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = """
            UPDATE clips
            SET is_pinned = CASE is_pinned WHEN 1 THEN 0 ELSE 1 END
            WHERE id = $id;
            """;
            command.Parameters.AddWithValue("$id", id);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task IncrementUseAsync(string id, CancellationToken cancellationToken = default)
        {
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = """
            UPDATE clips
            SET use_count = use_count + 1,
                last_used_at = $last_used_at
            WHERE id = $id;
            """;
            command.Parameters.AddWithValue("$id", id);
            command.Parameters.AddWithValue("$last_used_at", DateTimeOffset.UtcNow.ToString("O"));
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static void AddClipParameters(SqliteCommand command, ClipItem clip)
        {
            command.Parameters.AddWithValue("$id", clip.Id);
            command.Parameters.AddWithValue("$content", clip.Content);
            command.Parameters.AddWithValue("$content_hash", clip.ContentHash);
            command.Parameters.AddWithValue("$clip_type", clip.ClipType.ToString());
            AddNullableBlobParameter(command, "$binary_content", clip.BinaryContent);
            AddNullableBlobParameter(command, "$preview_content", clip.PreviewContent);
            command.Parameters.AddWithValue("$content_mime_type", (object?)clip.ContentMimeType ?? DBNull.Value);
            command.Parameters.AddWithValue("$image_width", (object?)clip.ImageWidth ?? DBNull.Value);
            command.Parameters.AddWithValue("$image_height", (object?)clip.ImageHeight ?? DBNull.Value);
            command.Parameters.AddWithValue("$title", (object?)clip.Title ?? DBNull.Value);
            command.Parameters.AddWithValue("$source_app", (object?)clip.SourceApp ?? DBNull.Value);
            command.Parameters.AddWithValue("$source_window_title", (object?)clip.SourceWindowTitle ?? DBNull.Value);
            command.Parameters.AddWithValue("$source_url", (object?)clip.SourceUrl ?? DBNull.Value);
            command.Parameters.AddWithValue("$is_pinned", clip.IsPinned ? 1 : 0);
            command.Parameters.AddWithValue("$created_at", clip.CreatedAt.ToString("O"));
            command.Parameters.AddWithValue("$last_used_at", clip.LastUsedAt?.ToString("O") ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("$use_count", clip.UseCount);
        }

        private static ClipItem ReadClip(SqliteDataReader reader)
        {
            return new ClipItem
            {
                Id = reader.GetString(0),
                Content = reader.GetString(1),
                ContentHash = reader.GetString(2),
                ClipType = Enum.TryParse<ClipType>(reader.GetString(3), out var clipType)
                    ? clipType
                    : ClipType.Unknown,
                BinaryContent = ReadNullableBlob(reader, 4),
                PreviewContent = ReadNullableBlob(reader, 5),
                ContentMimeType = reader.IsDBNull(6) ? null : reader.GetString(6),
                ImageWidth = reader.IsDBNull(7) ? null : reader.GetInt32(7),
                ImageHeight = reader.IsDBNull(8) ? null : reader.GetInt32(8),
                Title = reader.IsDBNull(9) ? null : reader.GetString(9),
                SourceApp = reader.IsDBNull(10) ? null : reader.GetString(10),
                SourceWindowTitle = reader.IsDBNull(11) ? null : reader.GetString(11),
                SourceUrl = reader.IsDBNull(12) ? null : reader.GetString(12),
                IsPinned = reader.GetInt32(13) == 1,
                CreatedAt = DateTimeOffset.Parse(reader.GetString(14)),
                LastUsedAt = reader.IsDBNull(15) ? null : DateTimeOffset.Parse(reader.GetString(15)),
                UseCount = reader.GetInt32(16)
            };
        }

        private static void AddNullableBlobParameter(SqliteCommand command, string name, byte[]? value)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.SqliteType = SqliteType.Blob;
            parameter.Value = value is null ? DBNull.Value : value;
            command.Parameters.Add(parameter);
        }

        private static byte[]? ReadNullableBlob(SqliteDataReader reader, int ordinal)
        {
            return reader.IsDBNull(ordinal) ? null : (byte[])reader.GetValue(ordinal);
        }
    }
}
