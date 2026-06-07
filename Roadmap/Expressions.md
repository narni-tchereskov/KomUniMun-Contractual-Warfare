# Expressions

This is a list of useful expressions for use in missions.

## Useful expressions

Orbited bodies except the sun
`OrbitedBodies().Where(b => !b.IsSun()).Random()`

Homeworld or its moons after being orbited
`OrbitedBodies().Where(b => b.IsHomeWorld() || (b.IsMoon() && b.Parent().IsHomeWorld())).Random()`

Parts with specific string in description
`AllParts().Where(b => b.IsUnlocked() && b.Description().Contains("STRING")).Random()`
