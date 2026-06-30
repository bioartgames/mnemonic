import React from 'react';

function IconBase({className, children}) {
  return (
    <svg
      className={className}
      width="24"
      height="24"
      viewBox="0 0 24 24"
      fill="none"
      stroke="#478cbf"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true">
      {children}
    </svg>
  );
}

export function RecordIcon({className}) {
  return (
    <IconBase className={className}>
      <circle cx="12" cy="12" r="8" />
      <circle cx="12" cy="12" r="3" fill="#478cbf" stroke="none" />
    </IconBase>
  );
}

export function ScoreIcon({className}) {
  return (
    <IconBase className={className}>
      <line x1="6" y1="18" x2="6" y2="14" />
      <line x1="12" y1="18" x2="12" y2="10" />
      <line x1="18" y1="18" x2="18" y2="6" />
    </IconBase>
  );
}

export function LinkIcon({className}) {
  return (
    <IconBase className={className}>
      <circle cx="7" cy="7" r="3" />
      <circle cx="17" cy="17" r="3" />
      <line x1="9.5" y1="9.5" x2="14.5" y2="14.5" />
    </IconBase>
  );
}

export function ReconstructIcon({className}) {
  return (
    <IconBase className={className}>
      <line x1="5" y1="7" x2="19" y2="7" />
      <line x1="5" y1="12" x2="19" y2="12" />
      <line x1="5" y1="17" x2="15" y2="17" />
    </IconBase>
  );
}
