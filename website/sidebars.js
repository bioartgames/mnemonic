// @ts-check

/** @type {import('@docusaurus/plugin-content-docs').SidebarsConfig} */
const sidebars = {
  docsSidebar: [
    'intro',
    'installation',
    {
      type: 'category',
      label: 'Concepts',
      collapsed: false,
      items: [
        'concepts/development-episodes',
        'concepts/automatic-capture',
        'concepts/git-linked-history',
        'concepts/narrative-reconstruction',
      ],
    },
    {
      type: 'category',
      label: 'Guides',
      items: [
        'guide/hook-dock',
        'guide/core-tray',
        'guide/heuristics',
        'guide/data-root',
      ],
    },
    {
      type: 'category',
      label: 'Reference',
      collapsed: false,
      items: [
        'faq',
        {type: 'link', label: 'Features', href: '/features'},
        {type: 'link', label: 'Roadmap', href: '/roadmap'},
      ],
    },
  ],
};

export default sidebars;
