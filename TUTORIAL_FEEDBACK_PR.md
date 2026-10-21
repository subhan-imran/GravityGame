# Teach three-jump travel and prevent early gravity escape

The takeoff-speed approximation could not guarantee that movement during jumps 1 or 2 stayed within gravity. Both lower arcs now include inward assistance and a source-planet height limit inside full-strength gravity. The next physics position accounts for tangential motion and pending movement/gravity impulses. Only a valid third grounded takeoff removes the constraint, allowing normal ballistic travel or lost-in-space defeat.

A nonblocking tutorial follows actual movement, takeoffs and landings. It can be skipped at any time and replayed from Pause without resetting exploration or star progress. Skip/completion is remembered on devices; editor checks do not write this preference. Jump chimes, short messages, a landing pulse, a gold third-jump cue and pooled star sparkles provide feedback. The effects respect pause and clear on retry.

Validation includes continuous lower-jump bounds on both planets, six radial directions, two physics step sizes, held running, extra outward velocity and unusually large jump heights. Existing third-jump crossings, loss/retry, star victory and rendered touch routing remain covered. Additional checks exercise tutorial progress, skip/replay, pause timing, duplicate subscription prevention and effect cleanup. Phone feel and audibility require device testing.

Local branch: codex/tutorial-feedback. Existing user-edited settings and package changes are preserved outside the feature commit. Generated caches, APKs, logs and screenshots are ignored by Git.
