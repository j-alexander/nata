module Nata.IO.HLS.Setup

// "brew install ffmpeg@7" if ffmpegloader is 7:
//  i.e. install ffmpeg 7.1.1 or similar to match
// see also:
// https://github.com/lethek/FFmpeg.Loader?tab=readme-ov-file
// https://github.com/Ruslan-B/FFmpeg.AutoGen/blob/f54e0b99207060878a1113383354f067c6267c4c/FFmpeg.AutoGen/generated/ffmpeg.libraries.g.cs#L4

let run() =
    FFmpeg.Loader.FFmpegLoader
        .SearchSystem()
        .ThenSearchPaths("/opt/homebrew/lib/") // Arm64 Mac
        .ThenSearchPaths("/opt/homebrew/bin/") // Arm64 Mac
        .ThenSearchApplication()
        .ThenSearchEnvironmentPaths("PATH")
        .ThenSearchEnvironmentPaths("FFMPEG_PATH")
        .Load()
        
