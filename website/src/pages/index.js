import clsx from 'clsx';
import useDocusaurusContext from '@docusaurus/useDocusaurusContext';
import useBaseUrl from '@docusaurus/useBaseUrl';
import Layout from '@theme/Layout';
import Heading from '@theme/Heading';
import {
  RecordIcon,
  ScoreIcon,
  LinkIcon,
  ReconstructIcon,
} from '@site/src/components/PillarIcons';
import styles from './index.module.css';

const pillars = [
  {
    Icon: RecordIcon,
    title: 'Record',
    description:
      'Playtests, saves, and commits — captured in time-bounded segments.',
  },
  {
    Icon: ScoreIcon,
    title: 'Score',
    description:
      'Heuristics rank each segment for significance; Mnemonic keeps what matters and logs the rest.',
  },
  {
    Icon: LinkIcon,
    title: 'Link',
    description:
      'Branch, commit hash, and subject attach to every preserved clip for traceable history.',
  },
  {
    Icon: ReconstructIcon,
    title: 'Reconstruct',
    description:
      'Browse, filter, and play clips to assemble devlog material.',
  },
];

function HomeHero() {
  const heroGif = useBaseUrl('/img/hero-dock.gif');
  const heroPoster = useBaseUrl('/img/hero-dock.webp');

  return (
    <header className={clsx('hero', styles.heroBanner)}>
      <div className="container">
        <div className={styles.heroInner}>
          <div>
            <Heading as="h1" className={styles.heroTitle}>
              Capture your devlogs while you work.
            </Heading>
            <p className={styles.heroSubtitle}>
              Mnemonic automatically captures development episodes with video, audio,
              metadata, and git context.
            </p>
            <p className={styles.heroPlatform}>Godot 4.6+ on Windows.</p>
          </div>
          <picture className={styles.heroPicture}>
            <source
              srcSet={heroGif}
              type="image/gif"
              media="(prefers-reduced-motion: no-preference)"
            />
            <img
              src={heroPoster}
              alt="Mnemonic dock in Godot during recording, showing LIVE timer and preserved clips with git commit subjects"
              className={styles.heroArt}
            />
          </picture>
        </div>
        <div className={styles.pillarsGrid}>
          {pillars.map((item) => (
            <article key={item.title} className={clsx('card', styles.card)}>
              <item.Icon className={styles.cardIcon} />
              <Heading as="h3">{item.title}</Heading>
              <p>{item.description}</p>
            </article>
          ))}
        </div>
      </div>
    </header>
  );
}

export default function Home() {
  const {siteConfig} = useDocusaurusContext();
  return (
    <Layout
      wrapperClassName="mnemonic-home"
      title="Development memory for Godot"
      description={siteConfig.tagline}>
      <HomeHero />
    </Layout>
  );
}
