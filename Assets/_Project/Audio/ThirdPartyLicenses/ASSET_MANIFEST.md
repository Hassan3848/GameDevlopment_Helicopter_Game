# Milestone 7 Audio Asset Manifest

Date downloaded: 2026-07-04

## Final assets

| Final file path | Original source website | Original clip title | Original filename | License type | Attribution required | Editing/conversion performed |
| --- | --- | --- | --- | --- | --- | --- |
| `Assets/_Project/Audio/Clips/Ambient/WindAmbienceLoop.wav` | `https://mixkit.co/free-sound-effects/wind/` | `Wind blowing ambience` | `2658.wav` | Mixkit Sound Effects Free License | No | Converted source WAV to PCM WAV 44.1 kHz with `ffmpeg` |
| `Assets/_Project/Audio/Clips/Combat/ExplosionSmall.wav` | `https://mixkit.co/free-sound-effects/explosion/` | `Bomb explosion in battle` | `2800.wav` | Mixkit Sound Effects Free License | No | Converted source WAV to PCM WAV 44.1 kHz with `ffmpeg` |
| `Assets/_Project/Audio/Clips/Combat/ExplosionLarge.wav` | `https://mixkit.co/free-sound-effects/explosion/` | `Car explosion debris` | `1562.wav` | Mixkit Sound Effects Free License | No | Converted source WAV to PCM WAV 44.1 kHz with `ffmpeg` |
| `Assets/_Project/Audio/Clips/Mission/VictoryStinger.wav` | `https://mixkit.co/free-sound-effects/discover/fanfare/` | `Grand brass fanfare` | `631.wav` | Mixkit Sound Effects Free License | No | Converted source WAV to PCM WAV 44.1 kHz with `ffmpeg` |
| `Assets/_Project/Audio/Clips/Mission/DefeatStinger.wav` | `https://mixkit.co/free-sound-effects/discover/dark/` | `Game over dark orchestra` | `633.wav` | Mixkit Sound Effects Free License | No | Converted source WAV to PCM WAV 44.1 kHz with `ffmpeg` |
| `Assets/_Project/Audio/Clips/UI/ButtonHover.wav` | `https://kenney.nl/assets/interface-sounds` | `Kenney Interface Sounds - tick_001` | `tick_001.ogg` | CC0 1.0 Universal | No | Converted source OGG to PCM WAV 44.1 kHz with `ffmpeg` |
| `Assets/_Project/Audio/Clips/UI/ButtonClick.wav` | `https://kenney.nl/assets/interface-sounds` | `Kenney Interface Sounds - select_001` | `select_001.ogg` | CC0 1.0 Universal | No | Converted source OGG to PCM WAV 44.1 kHz with `ffmpeg` |
| `Assets/_Project/Audio/Clips/UI/ButtonConfirm.wav` | `https://kenney.nl/assets/interface-sounds` | `Kenney Interface Sounds - confirmation_001` | `confirmation_001.ogg` | CC0 1.0 Universal | No | Converted source OGG to PCM WAV 44.1 kHz with `ffmpeg` |
| `Assets/_Project/Audio/Clips/Vehicles/HelicopterEngineLoop.wav` | `https://mixkit.co/free-sound-effects/helicopter/` | `Helicopter propellers in the sky` | `2704.wav` | Mixkit Sound Effects Free License | No | Converted source WAV to PCM WAV 44.1 kHz with `ffmpeg` |
| `Assets/_Project/Audio/Clips/Vehicles/TankEngineLoop.wav` | `https://mixkit.co/free-sound-effects/discover/tractor/` | `Tractors working on the field` | `1597.wav` | Mixkit Sound Effects Free License | No | Converted source WAV to PCM WAV 44.1 kHz with `ffmpeg`; selected as best heavy ground-vehicle fallback because Pixabay was blocked and no tank-specific Mixkit clip was available |
| `Assets/_Project/Audio/Clips/Weapons/MissileLaunch.wav` | `https://mixkit.co/free-sound-effects/whoosh/` | `Fast rocket whoosh` | `1714.wav` | Mixkit Sound Effects Free License | No | Converted source WAV to PCM WAV 44.1 kHz with `ffmpeg` |
| `Assets/_Project/Audio/Clips/Weapons/BombDrop.wav` | `https://mixkit.co/free-sound-effects/whoosh/` | `Air woosh` | `1489.wav` | Mixkit Sound Effects Free License | No | Converted source WAV to PCM WAV 44.1 kHz with `ffmpeg` |

## Source handling notes

- Kenney source archives were downloaded to a temporary directory outside the Unity project, inspected, and not copied into `Assets/`.
- Mixkit source WAV files were downloaded to a temporary directory outside the Unity project and converted into the final Unity-ready files.
- Pixabay was not used. Its official site returned a Cloudflare challenge in this environment, so no Pixabay assets were downloaded or copied.
