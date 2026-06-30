// @ts-check
import {createRequire} from 'node:module';
import {themes as prismThemes} from 'prism-react-renderer';

const require = createRequire(import.meta.url);

/** @type {import('@docusaurus/types').Config} */
const config = {
  title: 'Mnemonic',
  tagline: 'Automatically capture Godot development episodes with video, audio, metadata, and git context.',
  favicon: 'img/favicon.svg',

  url: 'https://bioartgames.github.io',
  baseUrl: '/mnemonic-hook/',

  organizationName: 'bioartgames',
  projectName: 'mnemonic-hook',

  onBrokenLinks: 'throw',

  i18n: {
    defaultLocale: 'en',
    locales: ['en'],
  },

  plugins: [
    [
      '@docusaurus/plugin-client-redirects',
      {
        redirects: [{from: '/faq', to: '/docs/faq'}],
      },
    ],
  ],

  presets: [
    [
      'classic',
      /** @type {import('@docusaurus/preset-classic').Options} */
      ({
        docs: {
          sidebarPath: './sidebars.js',
          editUrl: 'https://github.com/bioartgames/mnemonic-hook/tree/main/website/',
        },
        blog: false,
        theme: {
          customCss: './src/css/custom.css',
        },
      }),
    ],
  ],

  themes: [
    [
      require.resolve('@easyops-cn/docusaurus-search-local'),
      {
        hashed: true,
        language: ['en'],
        indexDocs: true,
        indexBlog: false,
        indexPages: true,
        docsRouteBasePath: 'docs',
      },
    ],
  ],

  themeConfig:
    /** @type {import('@docusaurus/preset-classic').ThemeConfig} */
    ({
      image: 'img/mnemonic-social-card.svg',
      colorMode: {
        defaultMode: 'dark',
        disableSwitch: true,
        respectPrefersColorScheme: false,
      },
      navbar: {
        title: 'Mnemonic',
        logo: {
          alt: 'Mnemonic logo',
          src: 'img/logo.svg',
        },
        items: [
          {
            href: 'https://github.com/bioartgames/mnemonic-hook/releases/latest',
            label: 'Download',
            position: 'left',
          },
          {to: '/docs/intro', label: 'Docs', position: 'left'},
          {to: '/docs/faq', label: 'FAQ', position: 'left'},
          {
            href: 'https://github.com/bioartgames/mnemonic-hook',
            label: 'GitHub',
            position: 'left',
          },
        ],
      },
      footer: {
        style: 'dark',
        links: [],
        copyright: `Copyright © ${new Date().getFullYear()} BioArt Games.`,
      },
      prism: {
        theme: prismThemes.github,
        darkTheme: prismThemes.dracula,
        additionalLanguages: ['powershell', 'bash', 'json'],
      },
    }),
};

export default config;
