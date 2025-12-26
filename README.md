# Renovated Breakout Game Using `vbPixelGameEngine`

![](screenshot.png)

## Description
This project is a revitalized version of the classic Breakout game, developed with [vbPixelGameEngine](https://github.com/DualBrain/vbPixelGameEngine). Adapted from the "Breakout2" sample project by the engine's creator, this version has been refined with creative enhancements like Attraction Mode and arcade-style gameplay mechanics.

Number patterns within the game have been restructured into a YAML file to avoid hardcoding. The game supports audio playback, while sprite-based graphics are not utilized. The background music is sourced from *Space Invaders '91* for the Sega Genesis, and sound effects are taken from *Arkanoid* for the Nintendo Entertainment System.

The player can score over 1000 points even though the scoreboard displays numbers in its current format. Each block is worth 1, 3, or 5 points. The game concludes when the player either successfully completes Level 9 or runs out of lives. Previous game data is saved and displayed on the screen after the game ends.

## Prerequisites
- [.NET SDK](https://dotnet.microsoft.com/en-us/download/): version 9.0 or later
- IDE: Visual Studio 2022 or Visual Studio Code
- Required NuGet packages: YamlDotNet 16.3.0 and NAudio 2.2.1

## How to Play
1. Clone this repository to your local machine:
```bash
git clone https://github.com/Pac-Dessert1436/Renovated-Breakout-Game.git
```
2. Restore project dependencies and build the project:
```bash
dotnet restore
dotnet build
```
3. Open the project in Visual Studio 2022 or Visual Studio Code.
4. Run the game using `dotnet run` in VS Code, or the "Run" button in Visual Studio 2022.
5. In-Game Controls (not shown in captions):
    | Key        | Action          |
    |------------|-----------------|
    | Left/Right | Move the paddle |
    | P          | Pause / Resume  |
    | ESC        | Exit the game   |

## Personal Notes
This Breakout game was developed when only 80 days remained until this year's Postgraduate Entrance Exam, as a fun project to pass the time. Right around China's National Day holiday back then, I still found programming far more engaging than poring over my TCM study materials.

Now that I've finished the exam, I don't feel confident about how I performed on the TCM sections, leaving my future hanging in the balance for the time being.

That said, contributing projects to GitHub has always been a great source of passion for me. I’m not sure if this enthusiasm will hold steady once I start working full-time, but I’m truly grateful to have this creative outlet, definitely a healthier alternative to venting frustration by raging at a screen while playing online games. For now, all I can do is keep coding as the long-time hobby it is, and wait for the exam results to come out.

## License
This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.