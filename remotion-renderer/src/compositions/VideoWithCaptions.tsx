import React from 'react';
import {
  AbsoluteFill,
  Video,
  useCurrentFrame,
  useVideoConfig,
  Sequence,
  staticFile,
} from 'remotion';

// Caption segment from MongoDB TranslatedSubtitles
export interface CaptionSegment {
  startMs: number;
  endMs: number;
  text: string;
}

export interface VideoWithCaptionsProps {
  videoUrl: string;
  captions: CaptionSegment[];
  captionStyle?: CaptionStyle;
}

export interface CaptionStyle {
  fontSize?: number;
  fontFamily?: string;
  color?: string;
  backgroundColor?: string;
  padding?: number;
  bottom?: number;
  textShadow?: string;
}

const defaultCaptionStyle: CaptionStyle = {
  fontSize: 32,
  fontFamily: 'Arial, sans-serif',
  color: '#FFFFFF',
  backgroundColor: 'rgba(0, 0, 0, 0.75)',
  padding: 12,
  bottom: 50,
  textShadow: '2px 2px 4px rgba(0,0,0,0.8)',
};

// Caption renderer component
const CaptionOverlay: React.FC<{
  captions: CaptionSegment[];
  style: CaptionStyle;
}> = ({ captions, style }) => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();
  const currentTimeMs = (frame / fps) * 1000;

  // Find current caption
  const currentCaption = captions.find(
    (cap) => currentTimeMs >= cap.startMs && currentTimeMs < cap.endMs
  );

  if (!currentCaption) {
    return null;
  }

  return (
    <div
      style={{
        position: 'absolute',
        bottom: style.bottom,
        left: 0,
        right: 0,
        display: 'flex',
        justifyContent: 'center',
        padding: '0 20px',
      }}
    >
      <div
        style={{
          fontSize: style.fontSize,
          fontFamily: style.fontFamily,
          color: style.color,
          backgroundColor: style.backgroundColor,
          padding: style.padding,
          borderRadius: 8,
          maxWidth: '80%',
          textAlign: 'center',
          textShadow: style.textShadow,
          lineHeight: 1.4,
        }}
      >
        {currentCaption.text}
      </div>
    </div>
  );
};

// Main composition
export const VideoWithCaptions: React.FC<VideoWithCaptionsProps> = ({
  videoUrl,
  captions,
  captionStyle,
}) => {
  const mergedStyle = { ...defaultCaptionStyle, ...captionStyle };

  return (
    <AbsoluteFill style={{ backgroundColor: 'black' }}>
      <Video
        src={videoUrl}
        style={{
          width: '100%',
          height: '100%',
          objectFit: 'contain',
        }}
      />
      <CaptionOverlay captions={captions} style={mergedStyle} />
    </AbsoluteFill>
  );
};

export default VideoWithCaptions;
