# Document pertaining to vessel spawning investigation

## Suspected causes

- Terrain
- Improper syntax
- Broken vessels

The vessels themselves may be broken, however, spawning over terrain seems to be
the root cause of all issues with spawning, explaining why test missions work as
intended whereas combat missions fail. Vessels spawned over terrain are nominally
moved to ground level which cancels the altitude parameters which are passed.

This may also be an issue with the syntax where the vessel spawning behaviour has
gained or lost parameters which i am currently unaware of.
