export const clipTypes = [
  "All",
  "Text",
  "Json",
  "Sql",
  "Url",
  "Jwt",
  "Guid",
  "Email",
  "Code",
  "Markdown",
  "Unknown",
];

export const devToolDefinitions = [
  { id: "guid", label: "GUID" },
  { id: "cpf", label: "CPF" },
  { id: "cnpj", label: "CNPJ" },
  { id: "lorem", label: "Lorem" },
  { id: "string", label: "String" },
  { id: "jwt", label: "JWT" },
  { id: "json", label: "JSON" },
  { id: "base64", label: "Base64" },
  { id: "url", label: "URL" },
  { id: "regex", label: "Regex" },
] as const;
