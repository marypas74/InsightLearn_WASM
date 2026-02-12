import React from 'react';
import { Composition } from 'remotion';
import { VideoWithCaptions, type VideoWithCaptionsProps } from './VideoWithCaptions';

// Default props for Remotion Studio preview
const defaultProps: VideoWithCaptionsProps = {
  videoUrl: 'https://archive.org/download/python_programming/python_programming_01.mp4',
  captions: [
    { startMs: 0, endMs: 3000, text: 'Benvenuto al corso di Python!' },
    { startMs: 3000, endMs: 6000, text: 'Oggi impareremo le basi.' },
    { startMs: 6000, endMs: 10000, text: 'Iniziamo con le variabili.' },
  ],
};

export const RemotionRoot: React.FC = () => {
  return (
    <>
      <Composition<VideoWithCaptionsProps>
        id="VideoWithCaptions"
        component={VideoWithCaptions}
        durationInFrames={30 * 60}
        fps={30}
        width={1920}
        height={1080}
        defaultProps={defaultProps}
      />
      <Composition<VideoWithCaptionsProps>
        id="VideoWithCaptions720p"
        component={VideoWithCaptions}
        durationInFrames={30 * 60}
        fps={30}
        width={1280}
        height={720}
        defaultProps={defaultProps}
      />
    </>
  );
};
