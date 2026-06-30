import clsx from 'clsx';
import Link from '@docusaurus/Link';
import Layout from '@theme/Layout';
import Heading from '@theme/Heading';
import styles from './features.module.css';

const features = [
  {
    label: 'Episodes',
    title: 'Development episodes',
    body: 'Mnemonic divides your session into segments — typically two minutes by default, configurable from 30 seconds to ten minutes. Each segment receives a significance score based on what happened during that window.',
    bullets: [
      'Preserved clips land in your local archive with video, thumbnail, and metadata',
      'Discarded segments stay in scratch until overwritten — nothing leaves your machine without a decision',
      'Manual preserve lets you flag the live segment anytime from the Hook dock',
    ],
    badges: ['Segment scoring', 'Preserve threshold', 'Segment log'],
    doc: '/docs/concepts/development-episodes',
  },
  {
    label: 'Capture',
    title: 'Automatic capture',
    body: 'Core runs as a Windows tray host alongside Godot. It captures your monitor, microphone, and optional system audio while the Hook addon streams editor events over a local IPC contract.',
    bullets: [
      'Start and stop recording from the Hook dock without leaving the editor',
      'Live preview row shows countdown, segment index, and running segment score',
      'Heuristic signals tune which editor events contribute to significance',
    ],
    badges: ['Screen capture', 'Mic + loopback', 'Editor signals'],
    doc: '/docs/concepts/automatic-capture',
  },
  {
    label: 'Git',
    title: 'Git-linked history',
    body: 'Mnemonic polls your repository during capture and attaches branch, commit hash, and commit subject to every clip. Commits and branch changes during a segment boost its score.',
    bullets: [
      'Filter clips and segment logs by branch or outcome',
      'Commit-after-playtest heuristic links iteration loops to version control',
      'Future phases will enrich clips with per-commit file lists',
    ],
    badges: ['Branch metadata', 'Commit subject', 'Git heuristics'],
    doc: '/docs/concepts/git-linked-history',
  },
  {
    label: 'Story',
    title: 'Narrative reconstruction',
    body: 'The Hook dock is your archive browser: search, filter, play video, and reveal clips in the file manager. Segment history shows kept and discarded outcomes with score breakdowns.',
    bullets: [
      'Clip tags summarize playtests, errors, saves, and iteration patterns',
      'Significance tiers help you focus on the moments that mattered',
      'Roadmap includes custom tagging and selected-clip export',
    ],
    badges: ['Clip browser', 'Segment log', 'Heuristic tags'],
    doc: '/docs/concepts/narrative-reconstruction',
  },
  {
    label: 'Editor',
    title: 'Godot-native Hook addon',
    body: 'Mnemonic Hook lives in your Godot editor as a dock panel. It defers heavy initialization until after the loading bar finishes, keeping editor startup snappy.',
    bullets: [
      'Scene tracker emits save and transition events',
      'Runtime error parser captures script failures during playtests',
      'Settings for Core path, auto-launch, capture retention, and heuristic toggles',
    ],
    badges: ['Editor dock', 'Godot 4.6+', 'IPC to Core'],
    doc: '/docs/guide/hook-dock',
  },
  {
    label: 'Privacy',
    title: 'Local-first archive',
    body: 'All data lives under your Windows user profile. No cloud account required. Mnemonic is a development memory system on your machine — your archive stays local.',
    bullets: [
      'DataRoot at %LOCALAPPDATA%\\Mnemonic\\',
      'JSON and JSONL files for clips, events, and segment history',
      'Single-instance Core mutex prevents duplicate capture hosts',
    ],
    badges: ['Local storage', 'No account', 'Your data'],
    doc: '/docs/guide/data-root',
  },
];

export default function Features() {
  return (
    <Layout
      title="Features"
      description="Development episodes, automatic capture, git-linked history, and narrative reconstruction for Godot.">
      <div className={styles.page}>
        <header className={styles.hero}>
          <div className="container">
            <Heading as="h1">Features</Heading>
            <p>
              Mnemonic is a calm, archival layer over your Godot workflow — capturing
              the texture of development, not just the artifacts.
            </p>
          </div>
        </header>

        <main className="container">
          <section className={styles.section}>
            {features.map((feature) => (
              <article key={feature.title} className={styles.feature}>
                <div>
                  <div className={styles.featureLabel}>{feature.label}</div>
                  <div className={styles.badgeRow}>
                    {feature.badges.map((badge) => (
                      <span key={badge} className={styles.badge}>
                        {badge}
                      </span>
                    ))}
                  </div>
                </div>
                <div>
                  <Heading as="h2">{feature.title}</Heading>
                  <p>{feature.body}</p>
                  <ul>
                    {feature.bullets.map((item) => (
                      <li key={item}>{item}</li>
                    ))}
                  </ul>
                  <p>
                    <Link to={feature.doc}>Read the docs →</Link>
                  </p>
                </div>
              </article>
            ))}
          </section>
        </main>
      </div>
    </Layout>
  );
}
