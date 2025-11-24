window.playbackHelpers = {
  getCurrentTime: (audioElement) => {
    return audioElement ? audioElement.currentTime : 0;
  },

  syncAudio: (audioElement, dto) => {
    if (!audioElement) return;

    audioElement.currentTime = dto.positionSeconds ?? 0;

    if (dto.status === 'Playing') {
      audioElement.play();
    } else if (dto.status === 'Paused') {
      audioElement.pause();
    } else if (dto.status === 'Stopped') {
      audioElement.pause();
      audioElement.currentTime = 0;
    }
  },

  // Optional: keep the "only one at a time" behavior
  exclusiveInit: () => {
    const audios = document.querySelectorAll('.jam-audio');
    if (!audios.length) return;

    audios.forEach(a => {
      a.addEventListener('play', () => {
        audios.forEach(b => {
          if (b !== a && !b.paused) {
            b.pause();
            b.currentTime = 0;
          }
        });
      });
    });
  }
};
