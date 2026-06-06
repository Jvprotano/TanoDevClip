export function summarize(content: string) {
  const compact = content.replace(/\s+/g, " ").trim();
  return compact.length > 72 ? `${compact.slice(0, 72)}...` : compact;
}

export function formatDate(value: string) {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return "";
  }

  return new Intl.DateTimeFormat(undefined, {
    month: "short",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
  }).format(date);
}
