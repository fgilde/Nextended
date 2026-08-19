import { fileURLToPath, URL } from "node:url";
import { defineConfig, type DefaultTheme } from "vitepress";
import packages from "../data/packages.json";

const REPO = "https://github.com/fgilde/Nextended";

// Served from https://fgilde.github.io/Nextended/ — a project page, so every asset and every
// link carries the repository name in front of it. Without the base the CSS and JS bundles
// resolve against the user page root and 404.
const BASE = "/Nextended/";

type Lang = "en" | "de";

/**
 * The package sidebar is built from the same docs/data/packages.json that feeds the README
 * generator, so a package added there appears in the navigation without a second edit.
 */
function packageSidebar(lang: Lang, prefix: string): DefaultTheme.SidebarItem[] {
  return packages.categories
    .map((cat) => ({
      text: cat[lang],
      collapsed: false,
      items: packages.packages
        .filter((p) => p.category === cat.id)
        .map((p) => ({
          // The full package name is long for a sidebar; the shared "Nextended." prefix goes.
          text: p.name.replace(/^Nextended\.?/, "") || "Core",
          link: `${prefix}/projects/${p.slug}`,
        })),
    }))
    .filter((group) => group.items.length > 0);
}

const EN_SIDEBAR: DefaultTheme.Sidebar = [
  {
    text: "Getting started",
    items: [
      { text: "Overview", link: "/" },
      { text: "Installation", link: "/guides/installation" },
      { text: "Architecture", link: "/guides/architecture" },
      { text: "Migration from nExt", link: "/guides/migration" },
    ],
  },
  {
    text: "Packages",
    items: [{ text: "All packages", link: "/projects/" }, ...packageSidebar("en", "")],
  },
  {
    text: "Examples",
    items: [{ text: "Common use cases", link: "/examples/common-use-cases" }],
  },
  {
    text: "API reference",
    items: [
      { text: "Extension methods", link: "/api/extensions" },
      { text: "Custom types", link: "/api/types" },
      { text: "Class mapping", link: "/api/class-mapping" },
      { text: "Helpers", link: "/api/helpers" },
      { text: "Encryption & hashing", link: "/api/encryption" },
    ],
  },
  {
    text: "Contributing",
    items: [{ text: "How the docs are generated", link: "/CONTRIBUTING" }],
  },
];

const DE_SIDEBAR: DefaultTheme.Sidebar = [
  {
    text: "Einstieg",
    items: [
      { text: "Überblick", link: "/de/" },
      { text: "Installation", link: "/de/guides/installation" },
      { text: "Architektur", link: "/de/guides/architecture" },
    ],
  },
  {
    text: "Pakete",
    items: [{ text: "Alle Pakete", link: "/de/projects/" }, ...packageSidebar("de", "/de")],
  },
  {
    text: "Beispiele",
    items: [{ text: "Typische Anwendungsfälle", link: "/de/examples/common-use-cases" }],
  },
  {
    text: "API-Referenz",
    items: [
      { text: "Extension Methods (en)", link: "/api/extensions" },
      { text: "Eigene Typen (en)", link: "/api/types" },
      { text: "Class Mapping (en)", link: "/api/class-mapping" },
      { text: "Helfer (en)", link: "/api/helpers" },
      { text: "Verschlüsselung (en)", link: "/api/encryption" },
    ],
  },
];

export default defineConfig({
  base: BASE,
  title: "Nextended",
  cleanUrls: true,
  lastUpdated: true,

  // The package listings are data-driven Vue in markdown; a stray unresolved link should fail
  // the build rather than ship a 404 like the old Jekyll site did.
  ignoreDeadLinks: false,

  // docs/data holds the generator's source of truth, not pages.
  srcExclude: ["data/**"],

  // Pages import the package data to render their listings. An alias keeps every page using the
  // same specifier instead of counting "../" levels per file, which is what broke the first
  // migration attempt.
  vite: {
    resolve: {
      alias: {
        "@data": fileURLToPath(new URL("../data", import.meta.url)),
      },
    },
  },

  head: [
    ["link", { rel: "icon", type: "image/png", href: `${BASE}icon.png` }],
    ["meta", { name: "theme-color", content: "#f97316" }],
    ["meta", { property: "og:type", content: "website" }],
    ["meta", { property: "og:image", content: `https://fgilde.github.io${BASE}icon.png` }],
  ],

  themeConfig: {
    logo: "/icon.png",
    socialLinks: [
      { icon: "github", link: REPO },
      { icon: "nuget", link: "https://www.nuget.org/packages?q=Nextended" },
    ],
    search: {
      provider: "local",
      options: {
        locales: {
          de: {
            translations: {
              button: { buttonText: "Suchen", buttonAriaLabel: "Suchen" },
              modal: {
                displayDetails: "Details anzeigen",
                resetButtonTitle: "Suche zurücksetzen",
                noResultsText: "Keine Ergebnisse für",
                footer: {
                  selectText: "auswählen",
                  navigateText: "navigieren",
                  closeText: "schließen",
                },
              },
            },
          },
        },
      },
    },
  },

  locales: {
    root: {
      label: "English",
      lang: "en-GB",
      description:
        "A suite of .NET libraries: extension methods, custom types, caching, EF Core, ASP.NET Core response filtering, source generation and .NET Aspire hosting integrations.",
      themeConfig: {
        nav: [
          { text: "Packages", link: "/projects/" },
          { text: "Installation", link: "/guides/installation" },
          { text: "Response filters", link: "/projects/responsefilters" },
          { text: "Source generation", link: "/projects/codegen" },
        ],
        sidebar: EN_SIDEBAR,
        editLink: {
          pattern: `${REPO}/edit/main/docs/:path`,
          text: "Edit this page on GitHub",
        },
        lastUpdatedText: "Last updated",
        footer: {
          message: "GPL-3.0-or-later",
          copyright: `Copyright © 2020–2026 <a href="${REPO}">fgilde</a>`,
        },
      },
    },

    de: {
      label: "Deutsch",
      lang: "de-DE",
      description:
        "Eine Sammlung von .NET-Bibliotheken: Extension Methods, eigene Typen, Caching, EF Core, Response-Shaping für ASP.NET Core, Codegenerierung und Hosting-Integrationen für .NET Aspire.",
      themeConfig: {
        nav: [
          { text: "Pakete", link: "/de/projects/" },
          { text: "Installation", link: "/de/guides/installation" },
          { text: "Response-Filter", link: "/de/projects/responsefilters" },
          { text: "Codegenerierung", link: "/de/projects/codegen" },
        ],
        sidebar: DE_SIDEBAR,
        editLink: {
          pattern: `${REPO}/edit/main/docs/:path`,
          text: "Diese Seite auf GitHub bearbeiten",
        },
        lastUpdatedText: "Zuletzt geändert",
        outline: { label: "Auf dieser Seite" },
        docFooter: { prev: "Zurück", next: "Weiter" },
        returnToTopLabel: "Nach oben",
        sidebarMenuLabel: "Kapitel",
        darkModeSwitchLabel: "Darstellung",
        langMenuLabel: "Sprache wechseln",
        footer: {
          message: "GPL-3.0-or-later",
          copyright: `Copyright © 2020–2026 <a href="${REPO}">fgilde</a>`,
        },
      },
    },
  },
});
