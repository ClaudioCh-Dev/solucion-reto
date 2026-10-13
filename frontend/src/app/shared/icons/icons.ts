// Íconos SVG en línea (stroke-current) para reutilizar con [innerHTML] | safeHtml.
const svg = (d: string, size = 20) =>
  `<svg width="${size}" height="${size}" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" xmlns="http://www.w3.org/2000/svg">${d}</svg>`;

export const ICONS = {
  box: svg('<path d="M3.5 7.5 12 3l8.5 4.5v9L12 21l-8.5-4.5v-9Z"/><path d="M3.5 7.5 12 12l8.5-4.5M12 12v9"/>', 24),
  edit: svg('<path d="M4 20h4L19 9a2.83 2.83 0 0 0-4-4L4 16v4Z"/><path d="m13.5 6.5 4 4"/>', 18),
  image: svg('<rect x="3.5" y="4.5" width="17" height="15" rx="2"/><circle cx="9" cy="10" r="1.8"/><path d="m20.5 16-5-5-8.5 8.5"/>', 18),
  refresh: svg('<path d="M20 11a8 8 0 0 0-14.6-4.5M4 4.5v4h4"/><path d="M4 13a8 8 0 0 0 14.6 4.5M20 19.5v-4h-4"/>', 18),
  arrowLeft: svg('<path d="M19 12H5M11 6l-6 6 6 6"/>', 18),
  logout: svg('<path d="M15 4.5h3A1.5 1.5 0 0 1 19.5 6v12a1.5 1.5 0 0 1-1.5 1.5h-3"/><path d="M10 16.5 5.5 12 10 7.5M5.5 12h10"/>', 24),
};
