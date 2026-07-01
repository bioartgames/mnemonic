import clsx from 'clsx';
import Layout from '@theme/Layout';
import Heading from '@theme/Heading';
import styles from './roadmap.module.css';

const phases = [
  {
    id: 'p1',
    title: 'Phase 1 — Core capture foundation',
    status: 'done',
    summary:
      'Windows tray host, FFmpeg capture, segment loop, git polling, heuristic scoring, clip index, and local DataRoot IPC.',
    items: [
      'Screen + microphone + loopback multitrack capture',
      'Segment close, preserve threshold, and scratch retention',
      'Git commit and branch change events',
      'Heuristic catalog: playtest, save burst, iteration cycle, runtime errors',
      'clip.json metadata and clips_index.json',
      'Tray UI with segment log and settings',
    ],
  },
  {
    id: 'p2',
    title: 'Phase 2 — Godot editor integration',
    status: 'done',
    summary:
      'Editor addon with dock UI, session event ingest, Core lifecycle, and end-to-end Godot + Core workflow.',
    items: [
      'JSONL session event append from Mnemonic to Core',
      'Status read, flag write, and graceful Core shutdown',
      'Mnemonic dock with clips list, filters, and live preview',
      'Start/stop recording from the editor',
      'Automated headless editor addon test runners',
    ],
  },
  {
    id: 'p3',
    title: 'Phase 3 — Development memory heuristics',
    status: 'done',
    summary:
      'Richer editor signals, clip grouping, significance tiers, and devlog-oriented suggestions.',
    items: [
      'Scene activity events and runtime error session events',
      'Heuristic score caps, dedupe, and iteration pattern derivation',
      'Clip grouping for iteration and error clusters',
      'Significance tiers in the Mnemonic dock',
      'Devlog suggestion scaffolding from grouped clips',
    ],
  },
  {
    id: 'p4',
    title: 'Phase 4 — Mnemonic dock UX & live capture',
    status: 'done',
    summary:
      'Polish the daily driver experience: live segment preview, overlay drawer, atomic IO, and workflow settings.',
    items: [
      'Live clip preview with segment countdown and running score',
      'Clips overlay drawer with search and filters',
      'Graceful Core shutdown and capture-session clip IDs',
      'Heuristic settings panel in the dock',
      'Atomic JSON IO for clip index operations',
    ],
  },
  {
    id: 'p5',
    title: 'Phase 5 — Custom clip tagging',
    status: 'planned',
    summary:
      'Let you label preserved clips with your own tags — beyond heuristic signals — for personal organization and devlog planning.',
    items: [
      'User-defined tags on individual clips',
      'Filter and search clips by custom tags in the Mnemonic dock',
      'Persist tags in clip metadata alongside heuristic tags',
    ],
  },
  {
    id: 'p6',
    title: 'Phase 6 — Selected-clip export',
    status: 'planned',
    summary:
      'Export clips you choose into a folder you pick — video and metadata together — for handoff to your editor or devlog toolchain.',
    items: [
      'Multi-select clips in the Mnemonic dock',
      'Export bundle to a user-chosen destination folder',
      'Include video files and sidecar metadata for each clip',
    ],
  },
  {
    id: 'p7',
    title: 'Phase 7 — Voice transcription (optional)',
    status: 'planned',
    summary:
      'Turn spoken commentary into searchable transcript excerpts attached to clips.',
    items: [
      'ClipIndex transcript excerpts and Mnemonic transcript search',
      'Transcript segment timestamps aligned to video',
      'Feed transcripts into optional AI summary fields',
    ],
  },
];

const statusClass = {
  done: styles.statusDone,
  active: styles.statusActive,
  planned: styles.statusPlanned,
};

const statusLabel = {
  done: 'Shipped',
  active: 'In progress',
  planned: 'Planned',
};

export default function Roadmap() {
  return (
    <Layout
      title="Roadmap"
      description="Mnemonic development roadmap — local-first capture today, curation and handoff next.">
      <div className={styles.page}>
        <header className={styles.hero}>
          <div className="container">
            <Heading as="h1">Roadmap</Heading>
            <p>
              Mnemonic is actively developed. Phases 1–4 are shipped — local-first
              capture with a browsable archive in Godot. Next up: curation and
              handoff so you can tag, select, and export clips on your own terms.
            </p>
          </div>
        </header>

        <main className="container">
          <div className={styles.legend}>
            <span>
              <span className={clsx(styles.dot, styles.dotDone)} /> Shipped
            </span>
            <span>
              <span className={clsx(styles.dot, styles.dotActive)} /> In progress
            </span>
            <span>
              <span className={clsx(styles.dot, styles.dotPlanned)} /> Planned
            </span>
          </div>

          {phases.map((phase) => (
            <article key={phase.id} className={styles.phase}>
              <div className={styles.phaseHeader}>
                <Heading as="h2">{phase.title}</Heading>
                <span className={clsx(styles.status, statusClass[phase.status])}>
                  {statusLabel[phase.status]}
                </span>
              </div>
              <p>{phase.summary}</p>
              <ul className={styles.itemList}>
                {phase.items.map((item) => (
                  <li key={item}>{item}</li>
                ))}
              </ul>
            </article>
          ))}
        </main>
      </div>
    </Layout>
  );
}
