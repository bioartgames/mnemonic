# Mnemonic Website

Official documentation site for [Mnemonic Hook](https://github.com/bioartgames/mnemonic-hook) — built with [Docusaurus](https://docusaurus.io/).

## Local development

```bash
cd website
npm install
npm start
```

Opens at [http://localhost:3000/mnemonic-hook/](http://localhost:3000/mnemonic-hook/).

## Build

```bash
npm run build
npm run serve
```

## GitHub Pages deployment

The site deploys automatically via GitHub Actions when changes under `website/` are pushed to `main`.

**One-time repo setup:**

1. Go to **Settings → Pages**
2. Set **Source** to **GitHub Actions**

Live URL: [https://bioartgames.github.io/mnemonic-hook/](https://bioartgames.github.io/mnemonic-hook/)

### Manual deploy (alternative)

```bash
GIT_USER=<github-username> npm run deploy
```

Requires `USE_SSH=true` or a GitHub token depending on your auth method. Prefer the GitHub Actions workflow for CI deploys.

## Structure

| Path | Content |
|------|---------|
| `src/pages/` | Landing, Features, Roadmap, FAQ |
| `docs/` | Installation guide and documentation |
| `static/` | Logo, favicon, social card |
| `docusaurus.config.js` | Site config (`baseUrl: /mnemonic/`) |
