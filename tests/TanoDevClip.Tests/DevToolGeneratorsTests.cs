using TanoDevClip.DevTools;

namespace TanoDevClip.Tests
{
    public sealed class DevToolGeneratorsTests
    {
        [Fact]
        public void Should_generate_random_string_with_requested_length()
        {
            var value = StringGenerator.GenerateRandomString(
                length: 24,
                includeUppercase: false,
                includeLowercase: true,
                includeNumbers: false,
                includeSymbols: false);

            Assert.Equal(24, value.Length);
            Assert.All(value, character => Assert.InRange(character, 'a', 'z'));
        }

        [Fact]
        public void Should_reject_random_string_without_character_groups()
        {
            Assert.Throws<InvalidOperationException>(() =>
                StringGenerator.GenerateRandomString(
                    length: 12,
                    includeUppercase: false,
                    includeLowercase: false,
                    includeNumbers: false,
                    includeSymbols: false));
        }

        [Fact]
        public void Should_generate_lorem_by_characters()
        {
            var value = LoremGenerator.GenerateByCharacters(32);

            Assert.True(value.Length <= 32);
            Assert.StartsWith("lorem", value);
        }

        [Fact]
        public void Should_generate_lorem_by_words()
        {
            var value = LoremGenerator.GenerateByWords(4);

            Assert.Equal("Lorem ipsum dolor sit.", value);
        }

        [Fact]
        public void Should_validate_generated_cpf()
        {
            var cpf = CpfTool.Generate(formatted: true);

            var result = CpfTool.Validate(cpf.Value);

            Assert.True(result.IsSuccess);
            Assert.Contains("valid", result.Value);
        }

        [Fact]
        public void Should_validate_generated_cnpj()
        {
            var cnpj = CnpjTool.Generate(formatted: true);

            var result = CnpjTool.Validate(cnpj.Value);

            Assert.True(result.IsSuccess);
            Assert.Contains("valid", result.Value);
        }

        [Fact]
        public void Should_format_json()
        {
            var result = JsonFormatter.Format("{\"ok\":true}", minify: false);

            Assert.True(result.IsSuccess);
            Assert.Contains(Environment.NewLine, result.Value);
            Assert.Contains("\"ok\": true", result.Value);
        }

        [Fact]
        public void Should_encode_and_decode_base64()
        {
            var encoded = Base64Tool.Encode("Olá TanoDev");

            var decoded = Base64Tool.Decode(encoded.Value);

            Assert.True(decoded.IsSuccess);
            Assert.Equal("Olá TanoDev", decoded.Value);
        }
    }
}

