# EscapeRoom

First-person escape room simulator. Unity 6000.3.5f2, URP, Input System 1.17.

The player walks (no jump, crouch or sprint), inspects objects with F, interacts and
picks things up with E, and backs out of any sub-view with Q. The character model stays
visible so the player can see their own arms and legs.

## Code style

Follow the conventions already in `Assets/Scripts/Player/`. In short:

- `_underscorePrefix` for private runtime state; plain `camelCase` for `[SerializeField]`
  fields, which are named for how they read in the Inspector. Write `private` explicitly.
- Constrain serialized numbers to the values that are actually valid, and nothing more.
  `[SerializeField, Min(0f)] private float moveSpeed = 2.5f;` — use `Min` rather than
  `Range`, unless there is a genuine ceiling. Store magnitudes as positive numbers and
  negate at the point of use, so `Min(0f)` still applies (e.g. `gravity = 15f`, used as `-_gravity`).
- Comment only what a reader would otherwise stop and question. No XML `<summary>` on
  MonoBehaviours — that is for library code consumed through IntelliSense. No `[Tooltip]`
  unless the field name genuinely cannot carry the meaning.
- Do not null-check something that cannot be null, and do not swallow a failure with an
  early `return`. Let it throw: the stack trace names the line, a silent zero does not.
- Do not split a short `Update` into named helpers. Ceremony reads as generated code.
- Keep classes to one job. Prefer another small component over a flag on an existing one.

Members go in this order, every time, skipping whatever a class does not have:

1. Constants — `const`, and `static readonly` such as cached `Animator.StringToHash` ids
2. `[SerializeField]` fields, grouped under `[Header]` where there is more than one group
3. Private runtime fields, and any private computed property that reads as derived state
4. Public events and properties
5. Unity lifecycle, in call order — `Awake`, `OnEnable`, `Start`, `Update`, `LateUpdate`, `OnDisable`, `OnDestroy`
6. Public methods
7. Private methods

Lifecycle methods stay together as one block; do not leave `Update` stranded at the
bottom of the file because it was written last. No `// 1. Constants` banner comments —
the order carries itself.

Name scripts the way the wider Unity community already names them — `MouseLook`,
`PlayerMovement`, `InputReader`, `GameManager`. Folders group them; they carry no
meaning to the compiler.

**No namespaces.** Every class sits in the global namespace by choice, so scripts never
need a `using` line to see each other. The cost is that an imported asset with a class
named `Door` or `Inventory` will collide and stop the project compiling. If that happens,
rename ours rather than reaching for a namespace — the rule is worth more than the
one collision it eventually costs.

## Input architecture

`InputReader` owns the single `PlayerControls` instance. Nothing else may call
`new PlayerControls()` — the game switches between three action maps, and a second
instance would leave maps enabled that the mode controller believes it disabled.

`PlayerModeController` is the only thing allowed to enable or disable an action map, to
set `Cursor.lockState`, or to toggle `MouseLook` and `PlayerMovement`. Those three move
together; splitting ownership is how you end up walking around with the inventory open.
It always clears the whole asset before enabling one map. To change what the player can
do, call `SwitchTo(PlayerMode...)` rather than reaching for a component's `enabled`.

How input is consumed depends on what moves the object, and the two must match:

| Mover | Runs in | Read input by |
|---|---|---|
| `CharacterController` | `Update` | polling `ReadValue()` |
| `Rigidbody` / `Rigidbody2D` | `FixedUpdate` | subscribing to `performed` + `canceled`, caching the value |

The project is set to `ProcessEventsInDynamicUpdate`, so input state refreshes once per
frame. `FixedUpdate` runs on its own schedule and may fire zero or several times per
frame, so code there must read a cached value rather than poll.

Polling needs no reset when a map is disabled — a disabled action returns `default`.
Any script that caches state in a field **does** need to clear it in `OnDisable`, or the
value survives into the next enable. This applies to accumulated velocity as much as to input.

Dispose `PlayerControls` in `OnDestroy`.

## Interaction

`PlayerInteractor` sphere-casts from the eyes each frame and hands E and F to whatever it
finds. It never branches on what kind of prop that is — a new prop implements
`IInteractable`, `IItemReceiver` or `IInspectable` and needs no change to the player.

**A prop is handed `IActor`, never `PlayerInteractor`.** That interface is three verbs —
`Say`, `OfferBag`, `Take` — and it is the whole of what a prop may ask of whoever is working
it. Every prop used to take the player class itself, so a clock, a drawer and a combination
wheel were each compiled against this game's player: fourteen files, for three verbs.

`IActor` lives beside `IInteractable` in `Interaction/` on purpose. `Clock` and `Puzzle`
already depend on that folder, so it costs no new dependency; put it with the player and the
coupling comes straight back under a different name. The measurable result is that `Clock`
and `Puzzle` now compile with no reference to `Player` at all, and a prop can be worked by
something that is not a player — a test that never opens a scene, for instance.

`InspectView` and `BorrowedProp` live in `Player/`, not `Interaction/`. They hold an
`InputReader` and a `PlayerModeController`; they were always player machinery filed in the
wrong drawer, and leaving them there is what kept `Interaction` pointing back at `Player`.

The cast stops on **everything solid**, not only the Interactable layer, then asks the hit
whether it is interesting. Masking to interactables alone lets the player take a key out of
a closed drawer through the cabinet wall.

## Looking at things

`InspectStyle` on `Inspectable` decides what F does, the same way `ClockCondition` decides
what a clock does. A new way of looking at something is a new case in that enum, not a new
class.

| Style | What happens | For |
|---|---|---|
| `Hold` | comes to the eyes and stays still | notes, tickets, coins, keys |
| `Turn` | comes to the eyes face on. **E turns it over; E again works whatever is round the back** | wall clocks, mantel clocks, pocket watches, alarm clocks — anything small enough to take down |
| `Zoom` | the prop does not move at all — the camera goes to it | cuckoos, the longcase, movements on stands — anything fixed where it is |

Two traps live here, and both have already bitten:

- **The anchor faces the same way the camera does.** A prop whose front is its local +Z
  needs `facing = (0, 180, 0)`, or it arrives showing its back. Zero is not a safe default,
  which is why the field ships at 180.
- **Anything a person could not lift must be `Zoom`.** A two-metre longcase clock yanked to
  your face is not a closer look.
- **A winder on the back means the clock must be `Turn`.** Wall and mantel clocks are wound
  from behind, which is where a clockmaker put it — and they can be taken down, so the back
  is reachable. A longcase cannot be taken down, so its winder stays on the front where
  the crosshair can find it, and a cuckoo has no winder at all — its chains hang below the
  case in a hull of their own. Get this pairing wrong and the crank becomes scenery: a
  mechanism the player can see and never touch.

**E works in two beats, and the second one follows from the prop.** The first press turns
the thing over — a clock arrives face on because that is how you tell which clock it is.
Once the back is showing, `InspectView` hands E to whatever `IInteractable` it found on the
prop: a `ClockWinder`, or nothing, in which case E just keeps turning it over. No second
enum decides this; what is bolted to the prop does.

The prompt line belongs to `PlayerInteractor`, which is switched off while something is
held, so `InspectView` writes it instead — otherwise the player is holding a clock with no
idea that E does anything. It writes it through **`ShowHandPrompt`**, which drops the line to
the foot of the screen: a held pocket watch covers the canvas from −267 up, a held wall clock
from −483, and a prompt sitting 90 px under the crosshair lands square on the dial the player
is trying to read. The message line has not been moved, so a prop that answers E by *saying*
something still says it across the middle.

**A held prop is placed by its middle, not its origin.** A wall clock hangs from a point at
the back of its case; aligning that point with the hold position put its head off the top of
the screen. The offset turns with the prop so the middle stays put however it faces.

`Zoom` runs in `PlayerMode.Zoomed`, which is `Inspecting` plus a live `PlayerInteractor`:
the crosshair still answers and E still reaches whatever is under it, so a clock can be
wound while you are leaning in at it. Held props get E through `InspectView` instead, since
the interactor is off in that mode.

Zoom closes in **along the line the player is already looking down, and never backs away**.
The goal used to be a fixed point in front of the prop, so walking right up to a clock and
then pressing F shoved the camera outwards past the player's own shoulders and the clock
appeared to be standing behind them. `Focus` on the Inspectable picks what is worth framing
— a dial, a window with a number in it — and the whole prop is used when it is left empty.

Hold distance scales with the prop's size. At one fixed arm's length a pocket watch is a dot
and a sheet of paper fills the screen.

Discrete presses (Interact, Inspect, Submit, Cancel, Navigate) subscribe to `performed`
rather than polling — a button read once per frame either repeats or gets missed. That is
a separate question from the Update/FixedUpdate rule above: continuous values poll,
discrete events subscribe.

## Clocks

The house is full of them, so they are built from parts rather than one class per clock.

- `ClockDial` points hands at a time it is handed and knows nothing else. It lives in its
  own prefab (`Assets/Prefabs/Clocks/ClockDial.prefab`) so a wristwatch, a pocket watch and
  a longcase can share it — all forty-seven clocks in the scene use that one prefab.

  It is one implementation of **`IClockDisplay`**, which is the seam the whole project is
  built to demonstrate: `ClockMovement` keeps a number of minutes and hands it to *every*
  display bolted under it, having never been told what a display is. `DigitalReadout` is the
  second implementation — four digits scrolling a printed strip, sharing not one line with
  the dial. The bench timing machine in the workshop is a movement driving both at once.

  **Displays are found, not wired.** There is no `dial` field. Parent a display under a
  movement and it starts showing the time; take it away and the clock keeps perfect time in
  private, which is what a movement with no hands does.

  Hand angles are **positive** about Z. The face looks down +Z and the player stands on
  that side, so from where they are a positive Z rotation sweeps the hand from the 12
  towards the 3. Reading it as anticlockwise is the mistake you make from the maths
  convention, where you are standing behind the dial; the hands ran backwards for a while
  because of exactly that. `ClockWinder` takes a positive angle for the same reason.

  To check it, never eyeball a hand against world axes — which way is "right" depends on
  which wall the clock hangs on. Derive the viewer's frame from the clock:
  `Vector3.Cross(Vector3.up, -clock.forward)` is their right, and the angle of `hand.up`
  in that frame is the position on the face.
- `ClockMovement` keeps a time and hands it to a dial. **It owns no power of its own.**
  Before it moves a hand it asks every `IClockPart` bolted to it, the same way `Openable`
  asks every `ILock`. Any one part can veto, and every part scales the rate.
  `ClockCondition` — `Running`, `Slow`, `Reversed`, `Stopped`, `Broken` — is the fiction on
  top: what is wrong with this clock, whatever it is built from. A sixth behaviour should be
  a new enum case, or a new part; not a new class.
- **A movement keeps its time in a `double`, and it is not optional.** A clock adds a frame's
  worth of minutes to however many minutes it already holds, and in a `float` those two
  numbers are too far apart to add: at 4:40 the total is 280, where one float to the next is
  0.00003 minutes, while a slow watch advances 0.00017 in a frame. A fifth of every step was
  being rounded away — and by a different amount on every clock, because the size of that gap
  depends on the time the clock is showing. Twelve identical watches given identical ten
  second runs came back with their hands between 59.3° and 61.3°, and the slow one at 22.1°
  where the puzzle wants 24. In a `double` the gap is 6e-14 and they land on 60.00 and 24.00.

  The public face stays `float` — `Minutes`, `Advance`, `Show` — because one cast at the edge
  costs 0.01° once, and it is the *accumulating* that cannot be done in a float. Anything
  that adds to `_minutes` has to stay in double the whole way, `Mathf.Repeat` included: it
  takes floats, so `OnDial` does the wrap instead.

  The general shape of it: **a running total and its increments have to be within a type's
  precision of each other.** Anything that integrates a small step into a large sum over
  thousands of frames is the same bug waiting.
- `Mainspring` is an `IClockPart` and an **`IPowerStore`**: the wound reserve. It is a
  component and not a field because a clock without one never runs down, which is right for
  weights or the mains.

  `IPowerStore` is named for the role and not the mechanism on purpose. Called `IMainspring`
  it would be a spring wearing an interface, and a weight line or a battery would be a
  second-class citizen; its methods are `Fill` and `Filled`, not `Wind` and `Wound`, for the
  same reason. `ClockWinder`, `WeightChain` and `ClockStriker` all find it with
  `GetComponentInChildren<IPowerStore>(true)` from the movement rather than holding a wire
  to it — three serialized references deleted, and a different kind of store would be picked
  up by all three without any of them changing.
- `Pendulum` is an `IClockPart` too. It has a `seized` flag, so jamming the pendulum stops a
  clock whose movement and spring are both fine — take the pendulum out of a longcase and
  the case is a cupboard. It also swings while the movement runs, which is the tell the hall
  puzzle depends on.
- `ClockWinder` is the crank. Its number reel deliberately does **not** consult the movement:
  a broken clock still shows a digit, which is what stops the reel from giving the puzzle away.
- `WeightChain` is the other way of putting power in: the chains under a cuckoo. Pulling them
  fills the same `Mainspring`, and the weights then ride at whatever height the charge puts
  them, so the thing that powers the clock is also the gauge for how much is left in it. A
  clock nobody has touched in years is one with its weights on the floor.
- `ClockStriker` opens and shuts a door once per hour on the dial. It listens to
  **`Mainspring.Wound`**, not to any particular handle, which is how the cuckoos traded their
  cranks for chains without it noticing. Anything that fills a spring sets the bird off.
  Winding being the trigger is the design, not a shortcut: the player chooses when to count.
- **The recording is the only clock in the strike.** The striker used to keep a stopwatch of
  its own — a stroke every 0.55 s — and a separate component cut one call out of the sound to
  fit each opening. That put the door on one rhythm and the voice on another: the door swung
  every 1.1 s while the recording called every 0.8, and every swing restarted the clip part
  way through. Nothing lined up, and it sounded like it.

  Now the door is a **pure read of the playhead**: open when the playhead is within
  `openLead` before a call and `openHold` after it, shut otherwise. The two cannot drift,
  because there is only one of them. Three numbers describe the recording — `firstCall`
  1.035 s, `callSpacing` 0.805 s, `callsInClip` 11 — and all three are measured off the clip
  rather than guessed at.

  **Getting one of them wrong is silent.** `firstCall` was set to 1.84 first, which is the
  *second* call: the bird then kept perfect time, one call behind the sound, all the way down
  — which is much harder to spot than being out of time. Each call is a loud "cuck" with a
  softer "oo" a quarter second after it, and it is the loud one that is the beat; the softer
  note is what a peak-picker set too high mistakes for the start of the next call.
- **The count is still the hour, and it is still what the parlour reads.** `Strikes` calls
  means `Strikes` openings, each 0.52 s long, each centred on a call in the recording. A
  clock wanting more strikes than the recording holds is clamped to what it can actually say;
  the house asks for five, nine and two.
- **Shut the door on the way out, both ways out.** The strike ends when the last window
  closes *or* when the clip runs out from under it — which is what happens if those numbers
  are set past what the recording holds. Leaving the door wherever it stood is how a bird
  ends up hanging out of its hole for the rest of the game.
- **`CuckooBird` is not `SlideMotion`, and that is the whole reason it exists.** A motion
  moves the thing it is bolted to according to the catch bolted beside it; a bird bolted to
  the door would swing aside *with* the door instead of coming out through it. So it follows
  a catch on another object and slides straight, and it needs no count of its own — it is out
  for exactly as long as the door is.

  The door's `swingSpeed` is 620 and the bird's 0.46 m/s because the recording leaves 0.28 s
  between one bird going in and the next call: at the old 320 the door was still shutting when
  it had to open again, and the whole thing read as a shuffle.
- **A cuckoo needs a hole to come out of.** The case was one solid block with a dark plate
  stuck on the front where the door was, which is fine for a door that only ever swings and
  useless for a bird. The front is built round the opening now the way a wall is built round
  a window — a carcass behind, four pieces of face with a hole between them, a dark panel at
  the back of the recess — and the surround is four bars rather than one plate, because a
  plate is the thing the bird would have to push through on its way out.

**A dial stuck to anything is already a clock.** Screw a `ClockDial` and a `ClockMovement`
to a plank and you have a clock face showing a time; whether it *goes* is decided entirely
by what else you bolt on. The two bare movements on stands — the one in the workshop vice
and the great one behind the case — have zero parts on purpose, which is why they sit at
whatever time they are set to and never drift.

The trap that comes with this: **absence cannot veto.** A movement with no parts runs
forever, because there is nothing there to say no. Every clock that the fiction calls
`Stopped` therefore needs a `Mainspring` on it, or it will quietly keep perfect time — which
is exactly how the twelve pocket watches nearly broke Act II.

For Act 1, decoy clocks must be `Broken`, not `Stopped` — a `Stopped` clock starts running
once wound, and then every clock in the hall is running and the tell is gone.

Where a winder goes follows how the clock is looked at — see **Looking at things**. Wall and
mantel clocks are wound from behind, so their winders are built with `Winder(..., onBack:
true)`, which mirrors every child offset in z. The longcase's crank stays in the hood, on the
front, because a two-metre case is `Zoom` and never turns round.

The cuckoos have no crank at all. Their chains hang below the case in their own `Weights`
hull, clear of the case's, so a low glance finds the chains and the dial above stays free
for `F`. That collider is what makes the chains aimable; without it they are decoration.

`Tools > Escape Room > Clocks > …` rebuilds both prefabs from `ClockBuilder`. Change the
geometry there rather than by hand, or the next rebuild throws the edit away.

## Things that open

Doors, drawers, lids and cabinet fronts are assembled the same way clocks are, out of one
component per job:

- `Openable` holds shut-or-open, carries the `[E]` prompt, and fires `Opened`. It moves
  nothing.
- `SwingMotion` / `SlideMotion` do the moving, reading `IsOpen` and knowing nothing about
  why. `SlideMotion` takes a vector, so a hatch that lifts is the same component pointed up.
- `ILock` is anything that can hold an `Openable` shut. `Openable` asks every lock fitted to
  it, so a case wanting both a key and a code needs no new class — just both components.
  `KeyLock` wants an `ItemData`; `CodeLock` reads a row of **`ICodeInput`**, so what the
  player actually turns is not its business — the wheels here step a printed strip round
  behind a window, and a keypad would drop in beside them unchanged.

A door that was never locked simply has no lock component on it. Adding a new kind of
opening thing should mean a new motion or a new lock, never a new `Door`.

## Puzzle 1 — the hall of clocks

Seven wall clocks. Four were still going when the player arrived; three are `Broken`.

The rule is: a swinging pendulum counts, a still one does not, and the ones that count are
read in the order their dials point to. Reading the four live clocks that way gives
**4719**, which opens the display case holding the brass key for the exit door.

**The room states that rule nowhere at present.** It used to be a pictogram
(`Assets/Textures/NotePage.png`, still drawn by `PuzzleBuilder`), and the note now carries
the shop's care leaflet in words instead — see below. The page the player reads is written,
not drawn, by decision: hints here are letters, not pictures. Whatever states the counting
rule next has to be written the same way.

Two things carry the design and are easy to break:

- Every clock shows a digit when cranked, including the dead ones. That is the trap. If the
  reel ever consulted the movement, the decoys would announce themselves.
- The hall clocks run at `minutesPerSecond = 1/60` — real time. At the default `1` the dials
  drift hours apart within minutes and the ordering the puzzle depends on scrambles itself.

**The note is the shop's care leaflet**, and every mark on it is type: *wind every clock,
weekly; a clock left stopped comes to harm*. That says in Thai why a player would go round
cranking seven clocks, which a pictogram never could, and it doubles as the fiction for why
three of them are dead.

**Words are UI; the sheet is a material.** Heading, rule and prose are live text under
`Face`, a world-space `Canvas` on the note holding two legacy `Text` components in
`LeelawadeeUI` — the same font and the same stack the HUD prompts already use. A `Color[]`
has no font engine, so type is the only thing that could put Thai on this paper at all, and
the rest follows: the words are data, changeable without redrawing anything, translatable.

The **border belongs to the paper, not to the canvas**. `TextureKit.Paper` draws the tooth
of the sheet and rules the border into the same map, so hanging another leaflet is a
material and nothing else — no four bars to lay over the prop afterwards, and no chance of
one being forgotten. It is drawn at the sheet's own aspect (1024 × 1408 for 260 × 360 mm),
which is what makes a pixel square and the border even on all four sides, and white like
every other map here, the border being its one darker value so it tints with the sheet.
`Retexture` draws it; nothing may tile it.

Set as a page rather than a label: the border sits 11 mm in, and the block between its
margins is balanced — 75 mm of clear paper above the heading and 75 below the last line —
because a short notice floated at the top of a big sheet reads as unfinished. The canvas is
**ten units to the millimetre**: at one to one a 14 mm heading asks the font for 14 pixels
and is then magnified a thousandfold onto the paper, which is a blur.

`NotePage.png` and `NotePage.mat` are now hung nowhere. `WriteNoteTexture` still draws the
pictogram, and until something wants it the file is a page nobody sees.

What the page says also lives on a `WrittenPage` bolted to the note, behind **`IReadable`**.
`InspectView` asks every prop it lifts whether it has one and hands it to `PagePanel`, so F
brings the paper up *and* puts the words on screen; Q takes both away. A prop without one is
every other prop in the house, which is why it asks rather than assumes.

Three traps came with it, all already paid for:

- **A canvas is read from its −Z side.** Face it at the player the way a quad would be faced
  and every glyph comes out mirrored; `Face` therefore carries a half turn about Y, exactly
  like a bare Unity `Quad`.
- **The note is an unscaled root now**, with the paper a child cube scaled 0.26 × 0.36 ×
  0.02. Hang a canvas under the scaled cube instead and it inherits the squash: 13:1 on z,
  and every glyph smeared.
- `PagePanel.Hide` checks its panel. Leaving play tears the HUD down before `InspectView`,
  whose `OnDisable` puts every page away on the way out, and a destroyed object is not a
  null field — this is the one shape of null check the house keeps.

**The coin says where the counting starts.** It lies on the showroom table with a compass rose
struck into its face, and `CompassNeedle` turns that rose to a bearing in the *world* every
frame — east, at the wall the coin is nearest, 1.40 m away against 2.25 north, 2.75 south and
4.60 west. Pick it up, carry it across the room, turn round and look again: it still points at
that wall. Which is the whole of what it says — count from this side — and why it could not
simply be drawn on.

**A rose, not an arrow.** The first mark was a plain black arrow and it read as a sticker:
near-black on gold is the hardest contrast in the house and nothing else here is marked that
way. The rose is the same information in the shop's own metal — eight points, each split into
a pale half and a dark one so the light does the drawing, the bearing's point longer than the
other seven, and a plain gold boss at the middle. Struck rather than printed, which is what a
coin is. `Tools > Escape Room > Puzzle > Build Coin Face` rebuilds the face and nothing else:
the root, the collider, the `Inspectable` and the bearing are all read off what is there and
put back, so it can be run again after the bearing has been pointed at another wall.

**Half metal, not all of it.** A fully metallic surface has no colour of its own — everything
it shows is reflection — and with no probe in that room the rose came out grey tin on a gold
coin. At 0.5 the gold in the base colour still carries.

Three things that placement needed:

- **A flat needle cannot point out of its own plane.** It turns in the coin's face and shows
  the bearing projected into it, so a coin held flat to the eye has one bearing it cannot
  show: the one straight through the player's head. The `Inspectable` therefore holds it at
  a slant — `facing (-45, 0, 0)`, a compass in the palm rather than a medal — and the face
  then carries 97% of the bearing even standing at the far side of the room facing away.
- **The coin's root was scaled 0.085 × 0.006 × 0.085**, so anything parented to it came out
  squashed fourteen to one. Same trap as the setting knobs, and the same fix: the disc moved
  into a child of its own and the root went back to unit scale. A prop that will ever carry a
  second part cannot wear its own scale on the root.
- **The rose is wound to face up: centre, then the vertex at the lesser angle, then the
  greater.** Rings in this project run from +z towards +x, and the other order gives a rose
  you can only see from underneath the table. When a flat mesh renders as nothing, check the
  winding against a triangle's own cross product rather than against the stored normals —
  `RecalculateNormals` will happily hand back a tidy `(0, 1, 0)` for a face pointing down.

`Tools > Escape Room > Puzzle > Build All` rebuilds the case, the key and that page.

## Puzzle 2 — the timing test

Twelve pocket watches on the workshop wall, each stopped at whatever time it came in at.
Eleven keep time; **`Watch_07` is `Slow`**, and its dial reads **4:40** — which is what the
movement in the vice wants, and what opens the bench drawer.

Nothing on the dials says which watch matters, and that is the point: the odd one out is
found by testing, not by reading. The test is Arthur's own, written on the note by the bench:
start the mechanism at the same moment as the glass, then compare.

How that plays, and what carries it:

| Step | What the player does | What answers |
|---|---|---|
| Take a watch down | F | `Inspectable`, style `Hold` |
| Hold the glass up beside it | F again | `HeldSandglass` on the camera, if the bag has the glass |
| Start both together | E | `TestRun` on the watch **and** the glass turn, on the one press |
| Read it | the sand runs 10 s; the second hand should be on the 2 | `slowRate 0.4` leaves the slow one at 24° and still moving |

- **`TestRun` is an `IClockPart`, so it starts nothing — it stops things.** It vetoes until a
  run begins and lifts the veto for the length of it, which is all "start the mechanism"
  means to a movement that is otherwise sound and wound. The watches are `Running` and
  `Slow`; what makes them stand still on the wall is this part saying no.
- **A test must not change what the watch says.** Every run puts the hands back to the time
  the watch was found at, so a dial reading 4:40 still reads 4:40 after twenty tests. That
  is what `ClockMovement.Minutes` is for.
- **The two starts are one press.** `InspectView` works the prop and turns the glass in the
  same call — a test where the mechanism and the glass start a fumble apart measures the
  fumble.
- **The run is ten seconds, the same ten the glass measures.** `runSeconds` was thirty, a
  safety net against a test nobody came back to, and it meant the hand carried on for twenty
  seconds after the sand had stopped: look late and you read a bigger angle than the answer.
  At ten the hand freezes on the reading and stays there to be read at leisure. The glass
  takes 0.35 s to come over before its sand starts, so it empties a blink after the hand
  stops; that is the flip, and it is the glass's business, not the watch's.
- **`RunTicking` is the sound of a run, and owns no length of its own.** It starts when
  `TestRun` lifts its veto and stops when the veto comes back, so the ticking cannot come
  apart from the test however long a run is set to. It is bolted beside `TestRun` on each of
  the twelve watches and brings its own `AudioSource`, which is the whole of fitting it —
  nothing in `InspectView` or on the player was touched to get it.

  The one number it owns is how much of the clip to use. `clock-ticking-down.mp3` is nine
  seconds against a ten second run, so a piece of it is played over and over rather than the
  whole thing once with a second of silence on the end — and that second is the one the
  player is staring hardest at the glass. **The piece has to be a whole number of ticks.**
  Measured off the clip, the ticks sit on a grid of 0.255 s, so 5.1 s is twenty of them and
  the join falls where a tick was going to fall anyway; 5.0 would swallow a beat every time
  round. Twice through covers the run with two tenths to spare.
- **Every watch ends on the 2. The tell is when it gets there, not where it stops.** A run
  ends when the hands have covered the ten minutes, so its length in real seconds is
  `runSeconds / ConditionRate`: ten for a sound movement, twenty-five for one running at four
  tenths. The sand runs out with eleven watches already still and one crawling on alone —
  measured at 40 fps, eleven stopped and Watch_07 still going at 47°, arriving at 60.00° some
  fifteen seconds later.

  **So the reading has to be taken while the glass is running.** The old behaviour froze the
  slow one at 24° and left it there to be found at leisure; now a player who wanders off and
  comes back sees twelve identical dials. That is the cost of the fiction — a movement being
  *held back* rather than stopped short — and it was chosen with that known.

  `ConditionRate` is public for this and only this. The parts are deliberately left out of
  it: one of those parts is the test doing the asking.
- **The last frame of a run is paid for in part.** `TestRun` returns a `RateScale` of what is
  left of the run divided by the frame about to be spent, so a frame straddling the end
  counts only as far as the end — the seam every part already uses to scale a movement, which
  is why the gate still drives nothing and the movement still decides how fast it goes.

  It counts in `LateUpdate`, not `Update`. Both it and `ClockMovement` used to count in
  `Update`, where the order between two components is undefined: whichever ran first decided
  whether the last frame of a run was spent or skipped, and which side of the mark the hand
  came to rest on was not ours to choose. `LateUpdate` is after every `Update`, so no Script
  Execution Order entry is needed.

  Measured on the twelve: every sound watch stops at 60.00°, and the slow one at 60.00° after
  25.01 s. Before the `double`, the same twelve landed anywhere from 59.3° to 61.3°.

  **Starting a run from anywhere but a key press can cost it one frame.** E arrives in the
  input phase, which Unity runs before every `Update`, so the frame the press lands in is
  spent by the movement and counted by the run alike. A run started from an editor script
  mid-frame can land *after* that movement's `Update`, and the frame is then counted but not
  spent: the hands come to rest at 59.85° instead of 60.00. It is an artefact of the way the
  measurement was taken, not of the run — worth knowing before believing a figure off a
  script that presses things itself.
- E reaches a held prop's `IInteractable` in `Hold` as well as `Turn` now — but in `Hold`
  **only an `IInHandOnly` answers**. `Turn` earns its E by having a back to reach, and a
  winder is what is round there; `Hold` has no such beat, so it takes only things meant to be
  worked in the hand. Letting it take any interactable put `PickupItem` on the end of E: the
  prop went into the bag while it was still up at the player's eyes, and the view stayed open
  holding an object that was no longer there, with walking still switched off. Every `Hold`
  prop that can be picked up — the coin, the keys, the clock parts, the glass — had it.
- **F has to come before E.** `TestRun` is an `IInHandOnly`, and `PlayerInteractor` drops
  those on the floor: the crosshair on a watch hanging on the wall offers `[F] ดูใกล้ๆ` and
  nothing else, and `InspectView` is the only thing that ever presses it. A movement is
  started on the bench, not by pointing at it across the room.
- **The glass is offered beside a `ITimedTest` and nothing else.** `InspectView` used to
  offer it beside every held prop once the bag had one, so a prism picked up to be counted
  came with a line about raising an hourglass — an instruction to do a thing that means
  nothing there. `TestRun` carries the interface; the prompt and `OnRaiseGlass` both check
  it, because a press that raises a glass nobody was offered is how a player learns to
  distrust the prompt line.
- **Q ends the run, on the frame it is pressed.** Put the watch back part way through and
  `TestRun.Abandon` stops it and puts the hands where it found them, so the wall never holds
  a watch quietly counting: its ticking would follow the player out of the workshop and its
  hands would walk on round the dial. `InspectView` asks for that through `ITimedTest`, the
  same interface that offered the glass — it is no longer an empty marker, and the player
  still knows nothing about what a run is. Anything else that wants working with a glass in
  the other hand gets the same cancel for free.

  On the frame rather than on the landing: the prop takes half a second to fly home, and
  half a second of ticking after the player has already said they are done is exactly the
  noise this was raised about.

## The sandglass

A ten-second glass on the rag on Arthur's bench (`Room_II_Workshop/Sandglass`, from
`Tools > Escape Room > Puzzle > Build Sandglass`). E collects it; from then on it is held up
with F while inspecting something and turned with E. It **tells nobody it has emptied** —
`Running` is the whole of its public face, and no puzzle asks it anything. A glass is a
length of time made visible; whether ten seconds was enough is the player's judgement, which
is the whole of Puzzle 2.

**The glass the player turns is not the one in the room.** `HeldSandglass` parks its own
instance in front of the camera, switched off, because the workshop's glass is in the bag by
the time anyone is timing anything. That copy carries no collider, no `PickupItem` and no
`Inspectable`: a solid prop a foot in front of the player's own crosshair is a prop that
answers every cast they make. It stays a **linked** prefab instance, and `HeldSandglass`
puts it on the overlay layer in `Awake` rather than the scene doing it — unpack it, or set
the layers by hand, and the next rebuild leaves the player holding the old glass, or a new
cage rendered into the room while the rest of it is in their hand.

The frame is two ash discs and a turned post either side, the way the old glasses were made.
Each post is beads and shafts mirrored about the waist, and the pair map onto each other
under the half turn — both of which the flip depends on.

**The glass is the whole shape**: one turned vessel between the discs, with nothing round it.
It is a lathed mesh (`Lathe`, saved to `Assets/Meshes/SandglassGlass.asset`)
rather than a stack of primitives, because on this prop the silhouette *is* the object and a
stack of cylinders shows a step at every joint down its edge. `Flare` is that outline: from
an 8.5 mm neck out to a 50 mm rim as `t^0.45`, which is what reads as an hourglass rather
than as two balls on a stick.

**Sand fills to a level; it does not shrink in place.** `Sandglass` turns its own two bodies
of sand every frame the level moves — the inside of one bulb, cut off level with however much
is left — from a profile the builder hands it as data, sampled off the same curve the glass
was turned from. Two wrong turnings were tried first and both are worth knowing:

- **Scaling one turned pile about its floor** squashes the shape and puts the wide top of a
  half-empty pile straight out through the side of the glass.
- **A stack of slices** cannot poke out, but it steps: the level jumps a slice at a time and
  the pile's silhouette is a staircase. Forty of them is also eighty renderers being switched
  on and off every frame, for a worse picture than one mesh gives.

The mesh way makes **an empty bulb look empty**, which is what the puzzle turns on: at ten
seconds the upper mesh has no triangles in it and its renderer is off, and the player can see
clean through the glass. Anything scaled to nothing still draws — as a disc of sand lying in
the top saying the glass has not finished.

**What falls through the neck is grains, not a rod of sand** — fourteen little cubes on a
loop, each dropping from the neck to whatever the pile has risen to, so the fall shortens as
the glass fills. It replaced a cylinder of sand standing in the neck, and costs less than the
cylinder did. Sand also heaps where it lands and funnels where it leaves: that is one number,
`heap`, pushing the middle of a surface up in the bulb filling and down in the one draining.

**Rings run from +Z towards +X, which is clockwise seen from above**, so `a-b-c` is the
winding that faces out and `a-c-b` faces in. Inside out, the sand is lit from behind and
reads as a black hole in the glass — and on a transparent shell like the vessel itself, it
does not look wrong at all until you put something solid inside it.

Three things in it are worth knowing before changing it:

- **The frame turns; the root does not.** All the geometry hangs off a `Frame` child, and
  that is what rotates, so the hull the crosshair aims at stays where the glass stands.
- **`Frame` is pivoted at the waist, not at the foot.** The builder lays the prop out from
  its base — which is how anything that stands on a table is measured — and then shifts the
  frame up to the middle and every part down by the same amount. Turned about its foot, the
  glass swings round in an arc like a signpost instead of coming over in the hand.
- **It is symmetric about the half turn** — caps, bulbs, and the three posts all land on
  themselves — so the turn ends by snapping `Frame` back to identity. Sand is then always
  drawn in a frame whose up is world up, and nothing has to track which way up it is. Add a
  fourth post, or anything that breaks that symmetry, and the snap becomes a visible jump.
- **The sand's geometry is authored, not written down twice.** The builder places both
  bodies of sand full and at rest; `Sandglass.Awake` reads the width, the height and each
  bulb's floor off them. Move a bulb in the builder and the sand follows.

`Sandglass` deliberately implements no interface. It carries a `PickupItem`, and two
`IInteractable`s on one prop is a coin toss over which one E reaches — so turning it is a
method call from whoever is holding it, not a press on the prop.

**It is carried but never offered.** `ItemData.UsedOnThings` is off for it alone, and
`InventoryUI` leaves anything with that off out of the list. The bag is only ever opened
because a lock or a socket asked what you want to use on it, and the glass answers nothing:
on that list it is one more wrong choice to scroll past on every door in the house. It stays
in `Inventory.Items` — carrying it is still what puts the held glass within reach — so what
changed is the offer, not the bag.

The list and the bag are two lists now, and `InventoryUI` keeps its own. Reading the choice
back out of `Inventory.Items` by row index worked only while every item was shown; leave one
out and the wrong key goes into the lock.

It lives under the room rather than under `Dressing`, so `Dress All Rooms` cannot take it
away. The rag under it *is* dressing, though — move that and the glass is left standing on
the bench where the cloth used to be.

## Puzzle 4 — the things that are missing

Act four's clue used to be the twelve alarm clocks: four had their levers thrown, and the
answer was their alarm hands read in hour order. That is gone. The wardrobe still holds the
longcase's pendulum and weights, and still wants four digits; what states them is now an old
photograph.

A framed photograph stands on the chest of drawers showing **that same chest with four
ordinary things on it**, left to right. The chest is bare now, and the four are round the
room: a teacup on the bedside table, a candlestick on the floor at the foot of the bed, a
vase on the clock shelf, a hand mirror lying on the quilt. Each has a **dealer's stock
ticket on the back of it** with a number on it. The wardrobe wants those numbers **in the
order the photograph stands them in** — `4 6 9 2` — and never the order they are lying about
the room in, which is the whole of it.

Nothing anywhere says so, and nothing may. The photograph carries no `WrittenPage`, and the
lock's refusal says only that the number is wrong. Working out that the picture is what puts
the four in order is the puzzle; state it in words and there is nothing left to solve.

**The picture is drawn from the props, not beside them.** `MissingThingsBuilder` builds each
object and then photographs it: every renderer becomes one mark on the print, at the tone of
the material it is made of and at that piece's own place in the object's frame. Change the
shape of the vase and the vase in the photograph changes with it — the same bargain `Stars`
makes with the constellation one room on, and for the same reason. A clue that can drift away
from the thing it is a clue to is not a clue.

**The answer is written down nowhere either.** `StampedNumber` holds one prop's digit and
prints it, off the same ten-digit strip a winder's number reel runs. `StampedCombination`,
bolted beside the `CodeLock` on the wardrobe, holds the four of them in the photograph's
order and hands the lock what that spells. So there are three things to change through the
Inspector and no code to edit: a prop's digit on the prop, the order in that one list, and
how many of them there are — add a `StampedNumber` to the list and a `CodeWheel` to the lock
and the puzzle is five digits long.

Four things this one paid for:

- **All four are modelled standing, front to local +z, ticket on −z.** That is what lets one
  pair of angles serve every one of them — `facing = (0, 180, 0)`, `backFacing = (0, 0, 0)` —
  and it is also the frame the photograph is taken in. A bare Unity quad faces its own −z and
  is read from that side, which is exactly where the player stands once the prop is turned
  over, so the ticket needs no half turn and the figure comes out the right way round.
- **None of the four may carry a `PickupItem`.** They are `Turn`, and `InspectView` hands E
  to any `IInteractable` it finds on a `Turn` prop: the thing would go in the bag while it
  was still up at the player's eyes. With nothing there, E turns it over and turns it back,
  which is the whole interaction.
- **A held prop backs off by its own size, so height costs legibility.** The candlestick was
  a quarter of a metre tall, which held it at arm's length and made the number on its foot
  three pixels of brass. It is squat now, with a deep plinth, because the ticket had to have
  somewhere flat and big enough to be read at the distance the prop's own height chooses.
- **Nothing goes in the corner by the wardrobe.** One lamp lights this room and that corner
  is in the wardrobe's shadow; a brass candlestick standing there is a thing the player walks
  past four times without ever seeing.

The alarm clocks are still in the room — twelve of them is who Arthur was — but they are all
one clock now: same hour, same lever, six to a board on the shelf. Four levers thrown and
four alarm hands spelling `3589` would be a second, wrong answer sitting in plain sight, and
a player who found it would conclude the game was broken. The chest is left bare on purpose:
the photograph and the surface it is a picture of have to be in one glance.

`Tools > Escape Room > Puzzle > Build Missing Things` rebuilds the print, the five props and
the wiring on the wardrobe.

## The birdcage

Act III's key sits in a **brass birdcage** on the parlour table, with a bird on the finial and
the three code wheels let into the drum it stands on. Turn the number and the cage door swings
open.

It is scenery doing the work a sentence would otherwise have to do. The room is three cuckoos
and a count of their calls, and nothing anywhere says the count is the number — but nothing
else in the house is shaped like this, and a player who has been counting birds knows where to
bring what they counted. **The clue itself has not moved**: it is still the clocks, still the
chains, still the calls. What changed is the thing the number opens.

`Tools > Escape Room > Puzzle > Build Strike Cage` rebuilds it. It keeps `BoxDoor`,
`Dialplate` and the key rather than making them again — those three carry the combination, the
wheels' printed strips and the key's own `ItemData`, none of which can be rebuilt from a
builder. Everything else under the prop is geometry and is thrown away each run.

The cage is one swept mesh: eighteen bars up the side and over the dome, four hoops, and a
door of five bars with an arc top and bottom on its own object so the swing carries it. One
`Sweep` draws all of it — a round section carried along a path, squared to the path from a
direction it is never parallel to, which is a bar bending over the dome and a hoop lying flat
without knowing the difference.

**Everything about the sizes was settled by casting, not by eye** — `PlayerInteractor`'s own
reach, aim assist and blocking layers, fired from where a player really stands. Four things
came out of it that no amount of looking would have found:

- **A cage is a container, so the bars carry a collider and the interior does not.** Aiming at
  the key with the door shut finds the door, which says the cage is locked; with it open, the
  key. That is the whole safety of the puzzle and it is a property of the geometry, not of any
  code: the bars sit 50 mm apart and the aim assist is a 120 mm sphere, so nothing gets in
  between them.
- **The key lies on the floor of the cage, and every attempt to raise it made things worse.**
  Hung off the perch it read as a wire — the bow of this key is a ring square to its shaft, so
  hanging it turns the ring edge on — and it measured worse besides: the higher it went, the
  more the sphere clipped the arc over the doorway. Down on the floor the sightline through the
  open door has 100 mm of air above and below it.
- **The door answers for the whole front of the cage.** Sized to the doorway alone it was
  nearly unaimable: a standing player looks *down* into a cage on a table, so a crosshair on
  the middle of it lands on the dome, and the dome is bars with no prompt on them. Its hit box
  therefore runs up to the spring of the dome and stands a little proud of the bars.
- **Which then swallowed the code wheels, and the wheels had to come forward.** A glance down
  at a wheel crosses the front of the cage a hand above the wheel itself, right through the
  door's box; 60 mm of aim assist clipped its corner and the door answered for the lock fitted
  to it. The wheels stand out in front by more than that radius now, on a brass block that
  carries them there without leaving a gap to see through.

The cage was also made taller than it looked right at — 370 mm to the spring of the dome
rather than 340 — purely so the sightline into the open door clears the arc from 0.8 m instead
of 1.0 m. Below about 0.65 m nothing works: the player is close enough to be looking through
the dome, and no shape short of an open-topped cage fixes that.

## Puzzle 3 — the Horologium

The last room, and by a long way the biggest: **9.2 × 7.5 m under a glass dome**, grown out
east and south from the cupboard it used to be, because an armillary with a beam across it
needs the floor. Growing it was two numbers and three wall runs in `HousePlan.asset` and a
rebuild — the way the house is meant to be moved.

**The dome is the roof, not a lamp hung under one.** `HousePlan.Room` carries a `Roofless`
flag and `HouseBuilder` skips the ceiling slab for a room that has it; the dome then springs
straight off the wall tops. Cap the room in plaster first and the dome is a glasshouse over a
lid. The great movement that used to stand here is gone with the puzzle it belonged to.

An armillary sundial stands in the middle of it: wrought legs on a stone slab, two standing
rings, a brass hour band square to the axis, and the axis itself through the heart of it —
**the three prisms ride that axis**, which is why they sit at the centre of the machine and
not up a post. A glass dome hangs from the ceiling above with the moon in it, and the
moonbeam comes straight down into the rings; the lens on the front of the ring throws what
comes out at a screen on the south wall, with six marks on it — Horologium, the pendulum
clock, which is the one constellation this house was always going to end on.

Arthur's 11:57 is gone from these walls. It was the old answer, and a wall shouting a number
at a puzzle that no longer wants one is a wall telling the player a lie.

**The prisms are not in this room.** They are put away in three containers, one to a room, and
the player has been carrying them for most of the game before finding out what they are for:

| Prism | Where | Available from |
|---|---|---|
| six-sided | the cabinet drawer in หน้าร้าน | the start — that drawer has no lock on it |
| four-sided | the bench drawer in ห้องช่าง | act two, with the parlour key |
| three-sided | the wardrobe shelf in ห้องนอน | act four — behind the code, beside the pendulum |

That is three things found in passing and one room at the end that explains all of them, rather
than three stones on the floor of the room that wants them. Fitted, the train lights and throws
six points at the screen; until then it is a dead machine and the prisms will not turn.

**`Build Horologium` writes the prism prefabs and does not place them.** Where they lie is a
scene decision, which is why they could be moved at all — but it also means nothing puts them
back if they are deleted. If a room is ever rebuilt out from under one, put the prefab back
from `Assets/Prefabs/Puzzle/Prism*Prop.prefab`.

**A prism is a piece of the night sky**: near-opaque midnight blue (`Gem`) with a scatter of
little bright spheres set inside it (`Sparks`). Three shades of the one colour, so they tell
apart on the gnomon without any of them being the odd one out, and the stars are what make
them findable in a dark drawer.

Three numbers in that material are load-bearing, and all three were got wrong first:

- **Emission lifts the colour, so it stays under a quarter.** At half strength a midnight
  blue renders pale lavender — which is not the colour the material says it is.
- **Smoothness stays low (0.34).** A polished flat face takes a broad highlight off the
  nearest candle, and on a face this small the highlight *is* the face: at 0.88 three
  midnight stones read as steel blue from a stride away.
- **Alpha is 0.94, and the stars are lit to carry through it.** What shows through a
  translucent stone is the room behind it, and this room has candlelit plaster in it: at 0.88
  a dark blue prism held up against a wall came back out grey. Density is most of what makes
  it read as stone, so the shell went denser and the sparks brighter together.

The stars are spheres and small (2.6–6.2 mm). A cube of a centimetre seen through a face at
arm's length is a white square with corners on it, which reads as a chip in the stone; a
scatter of little spheres reads as a sky. They carry no colliders, so the crosshair still
finds the prism's own box and nothing else.

**A prism is held cut-face towards the eye** (`facing = (62, 200, 0)`). How many sides it has
is the one thing a player has to read off it — which socket it will go in — and a prism held
upright shows its sides edge-on and its face not at all. The twenty degrees off the half turn
leave one side face showing under the cut, so it reads as a solid rather than as a card.

**Beside the screen hangs Arthur's chart of the constellation**, six stars joined up the way a
sky atlas joins them, and a `WrittenPage` on it so F puts what it says on the HUD as well. The
marks on the screen are the answer, but a ring of dark paint on a dark screen states it in a
whisper — the marks now carry a little emission of their own for the same reason. The chart is
drawn from `Stars()`, the same array the marks are cut from, so the answer on the wall cannot
drift away from the answer on the screen. That is the whole reason that method exists.

**Each prism does one thing to the pattern and nothing else.** The first turns it, a third of
a circle at a time; the second slides it sideways; the third opens it out. That is the whole
puzzle: a player who turns one and watches the wall learns what that one is for, and can then
solve the three separately instead of hunting through seventy-two combinations. Difficulty
from watching, not from arithmetic.

- **The light is not simulated and must not be.** `Horologium` reads the three prisms as how
  far each stands from where it belongs, and puts the marks through that: turn, slide, spread.
  Every prism at its answer leaves the shape exactly as it was drawn.
- **Six points on six marks, held for two seconds.** They glide rather than snap, so a pattern
  that is only right in passing is not the pattern. Then the prisms lock: a mechanism that has
  done its work does not come apart again.
- **`StarLock` is one of six kinds of catch** on an `Openable`, beside `KeyLock`, `CodeLock`,
  `TimeLock`, `RunningLock` and `SealedLock`, and the door has never been told what any of
  them are. It replaced the `TimeLock` that used to want 11:57 on `Door_Out`.
- The lights sit a few millimetres proud of the marks, and the rig keeps them there —
  `Horologium` reads that lift off the prop rather than writing it down again, and the
  alignment is measured across the screen only. Flatten them into one plane and the bright
  spot and the mark fight for the same pixels.
- **A hull round the machine is a hull round its sockets.** The armillary had one, and the
  crosshair found the sundial every time and the prism sockets never: three prisms in the bag
  and nowhere to put them. It is solid at the foot and up the stem only — the rings are open
  to walk through, and that is the price of being able to reach into the middle of them.
  Same rule as the drawers and the wardrobe, and the fourth time this project has paid it.
- **The same rule one level down: a socket collider must not cover its own prism.** The
  crosshair finds an interactable by walking *up* from what it hit, so a socket big enough to
  cover the prism inside it answers for ever after — the prisms went in and could never be
  turned, which is a puzzle with no second half. The socket's collider is the seat, a
  finger's depth under the prism; the prism carries its own, and is switched off until it is
  fitted, so the two never compete for the same press.
- **The machine is built at eye height, and nothing may stand in front of it.** The rings are
  centred at 1.44 so the three prisms ride at 1.19, 1.40 and 1.61 — the player looks straight
  at them instead of down 30 degrees at their own feet, and they sit a hand's width apart so
  the fringe of one hit box is not over the next. The moonbeam comes down to the top of the
  rings and stops: carried on into the middle of the machine it is a bright column standing in
  front of the three things the player is there to see and press.
- **`Build Shell` used to look the shell up by path**, which cannot see it while it is
  switched off: rebuilding with the shell hidden left a second one beside the first, walls
  standing through the middle of rooms. It looks through the parent now, so a hidden shell is
  still found and replaced.

`Tools > Escape Room > Puzzle > Build Horologium` rebuilds the sundial, the screen and the
three loose prisms.

## The house

Five rooms in a rough square, chained so there is exactly one route through:
**I → II → III → IV → V**, each door opened by what the room before it gave you.

```
                                z = 9.6
   +-------------------+--------------------+
   |  II  ห้องช่าง       |  III ห้องนั่งเล่น     |
   |  6 x 6.8          |  6.8 x 6.8         |
   +--------[door]-----+--------+-----------+  z = 2.6
   |                   |     [door]         |          |
   |  I  หน้าร้าน        |  IV  ห้องนอน        |  V  หลังตู้นาฬิกา    |
   |  6 x 5            |  6.8 x 5           |  9.2 x 7.5         |
   +-------------------+--------------------+  under glass       |
                                            +--------------------+  z = -5
 x = -3.1           x = 3.1            x = 10.1              x = 19.5
```

`HouseBuilder` reads the plan from `Assets/Recipes/HousePlan.asset` — five rectangles and
a list of wall runs with doorway centres — and generates floors, ceilings and walls from it.
The numbers used to be C# arrays in the builder, which made moving a room a code change; a
rebuild from the asset reproduces the old shell object for object, 193 of 193. Moving a room means
editing two numbers, not dragging thirty boxes. `Tools > Escape Room > House > Build Shell`
rebuilds it, and only ever touches `House/Shell`; everything under `House/Props` is left
alone.

Each room has its own floor material. It is the cheapest way to tell the player they have
crossed into somewhere new, and it costs nothing at runtime.

The way into V is a slice of wall with the longcase clock mounted on it — `SecretPanel`,
one `Openable` carrying both. The clock is a child of the panel rather than standing beside
it, which is the whole trick: the wall and the clock swing as one thing.

`HouseBuilder` also cuts windows and hangs the trim: skirting on both faces of every wall
run, architrave round every opening, a sill under every window, and a dark plane outside the
glass so a window is a view rather than a hole onto nothing. Each exterior wall carries an
`Outward` sign for that — get it wrong and the night sky lands in the middle of the room.

Interior doors are `DoorLeaf.prefab` plus `Openable` + `SwingMotion`, hinged at the leaf's
own origin and built along local +x like everything else that swings.

**The leaf is sized off the plan, and leaves an even 6 mm reveal.** `BuildDoorLeaf` reads
`DoorWidth` and `DoorTop` and takes the reveal off both sides and the head, so a door dropped
at the edge of its opening fills it. It used to be 1.06 in a 1.10 opening, hinged on one edge
of that opening, which put all 40 mm of the difference in a single slot down the closing edge:
the strip of daylight a set built out of boxes gives itself away by, moved sideways. Only the
hinge barrels stay on the opening edge, standing proud of the frame, where a real barrel is.

**A door must be a prefab instance, not a copy of one.** `Instantiate` on a door already in
the scene gives a disconnected object that looks identical and then quietly stops taking
rebuilds — the showroom door was made that way and was the one leaf still 1.06 wide after the
prefab was rebuilt. Use `PrefabUtility.InstantiatePrefab`, then add `Openable`, the motion and
the lock to the instance.

Nothing may stand in a doorway. Three alarm clocks ended up on the floor in front of
openings and blocked them; check with a `CapsuleCast` at the player's own radius rather than
by eye.

**And a doorway must have its door in it.** The leaves are placed in the scene, the openings
come out of the plan, and nothing checks that the two agree: moving room V east left the way
out standing free in the middle of the floor with a hole in the wall four metres behind it.
A leaf sits at the wall's own `Fixed` coordinate, hinged on the edge of its opening — for the
run at x 19.5 with a door centred on z 1.4 that is `(19.5, 0, 0.85)` turned 270°. After any
change to `HousePlan.asset`, check every leaf against the opening it belongs to.

The showroom door was the last one still built the old way — a single box 0.96 × 2.02 in a
1.10 × 2.15 opening, so it stood in daylight on three sides, and it was the first door in the
game. All four are `DoorLeaf.prefab` now. A new door is a copy of one that works, moved, with
its own key and swing; there is nothing else to it.

**The fifth door opens on nothing, and that is the whole of it.** `Door_Front` is the shop's
street door, in the south wall of the showroom where a window used to be — the opening the
player is looking straight at when the title card comes down. It carries a `BoxCollider` and
nothing else: no `Openable`, no motion, no lock, so the crosshair finds no prompt on it and
the player is stopped by it. The way out is at the far end of room five, and a front door that
answers E is a front door the player spends the game coming back to try.

It is `ShopDoor.prefab`, not `DoorLeaf.prefab`: glazed in four panes, with a letterplate,
which is what tells a street door from the four panelled ones inside. **The night behind its
glass is part of the prefab.** The plan calls this opening a doorway, and only a window gets a
painted night cut for it by `HouseBuilder`, so the door brings its own — hung a little further
out than the neighbouring window's so the two never share a plane and flicker.

The mat inside it is the reason it reads as the entrance rather than as a door somebody
bricked up. A door that says nothing and never moves needs something on the floor in front of
it doing the saying; nobody lays a welcome mat inside a room.

Changing that window to a doorway is one field in `HousePlan.asset` — `Kind: 1` to `Kind: 0`
on the opening at centre 1.3 — and a `Build Shell`. The doorway is 1.10 wide against the
window's 1.30, so the wall either side of it grew; check nothing was hanging there first.

**A container must not enclose what is inside it.** Every one of the three keys was, at some
point, unreachable because its own drawer or case had a collider wrapping the whole volume —
the crosshair found the box, never the key. A container's collider belongs on the part you
click (the drawer front, the door leaf), not round the compartment, and nothing on it may
stand taller than what is lying inside, or a downward glance grazes the top edge instead.

This rule was written down and then broken again on the very next container built: the
wardrobe shipped with one hull round its whole volume, and the two clock parts on its shelf
were unreachable. A cabinet's colliders go on its boards — back, sides, top, bottom — and
the space between them stays empty.

**And the opposite failure, on the same wardrobe: a front that was never finished.** Its
carcass was 1.30 wide and its door only 0.82, and nothing filled the remaining 0.48 — so it
stood with a hole down one side of its front and the combination plate floating in the middle
of that hole with nothing behind it.

It is rebuilt to the proportions of a real single-door wardrobe: **0.96 wide, 0.58 deep, 2.06
tall, one full-height door over one drawer**, on a plinth and under a cornice, with a shelf
and a hanging rail inside. Filling the hole with a fixed panel was tried first and is the
wrong answer — a third of the front given over to carrying a lock, and everything behind that
panel unreachable for good. **The plate belongs on the leaf it locks.** Mounted there it needs
no panel, the door can be full width, and it reads as what it is.

Two things carry the fiction, and they are separate on purpose:

- **The drawer is not locked at all.** The clock's weights lie in it and can be had the moment
  the player finds the wardrobe.
- **The pendulum and the prism are behind the code.** With the door shut, the crosshair on
  either of them finds the door and is told the wardrobe is locked; with it open, it finds
  them. Verified by casting, at 0.65 m and out.

Two hit-box lessons, both the same lesson as the birdcage's:

- **The door's box stops 100 mm above the drawer front.** Carried down to the foot of the
  leaf, a glance down at the drawer clipped its bottom edge and the door answered for a
  drawer that is not even locked.
- **A part hung off a rotated leaf inherits that rotation.** The door is hung at 270°, so its
  own +z runs *into* the cabinet: the plate, the knob and the raised panel all went in behind
  the leaf on the first pass, and the plate needed a half turn of its own to look out of the
  door — which also puts its wheels back in the order they were authored in, first digit on
  the viewer's left.

The door's shut position is **270° of yaw, and it is authored on the transform, not in code**.
`SwingMotion` reads `localEulerAngles.y` in `Awake` and swings `openAngle` from wherever it
finds the leaf, so zeroing that rotation to take a screenshot — which is exactly how it got
zeroed once — leaves the door standing at right angles to the front of its own carcass and
`Awake` then treating that as shut. Put a leaf's rotation back before saving the scene.

**Never place a control relative to a clock's transform.** The setting knobs were parented
to the movements and inherited whatever those had: the bench clock's origin is on the floor,
so its knobs ended up at y = −0.115, buried, and act two was unplayable. The great movement
is scaled 3.2, so its knobs flew 1.28 m apart. Knobs now live on an unscaled `SettingKnobs`
holder, positioned in world.

**Not parented to the clock, but placed off it.** Free of the clock's transform they drifted
the other way: they sat out on the bench a hand's width in front of the movement, a third of
a metre below it and a quarter out to either side, facing along the bench rather than at the
player — two brass things somebody had left lying about rather than the controls of the clock
between them. They are on the movement's own front plate now, either side of the arbor, where
the winding squares of a real movement are. The world position is still computed rather than
typed: `ViceAt`, plus the 1.15 the movement is seated at, plus the 0.16 its dial stands above
that. Placing *from* a clock is not the same as parenting *to* one.

Two things that placement had to answer:

- **Above the arbor, not below it.** Below is where a real movement puts them, and it is
  exactly where this one cannot: act two's answer is 4:40, and at 4:40 the hour hand lies 40°
  off straight down to the right and the minute hand 60° off it to the left — within a few
  degrees of two knobs set below the arbor. They would have covered the one reading the whole
  puzzle turns on. Up at the ten and the two the hands only cross them on the way round,
  while the player is pressing rather than reading.
- **Their size is a dial measurement, not a knob measurement.** At 70 mm across on a 270 mm
  face they were two brass lids over the five and the seven. A winding square is a third of
  that, and their hit boxes came down with them: at 0.13 the boxes reached back through the
  dial and the crosshair took a knob whenever it was pointed anywhere near the bottom of it,
  which is a movement nobody can pull up close.

**Test reachability through the real code path.** Sweeping "eight directions at 0.85 m from
the collider centre" gave both false negatives (positions inside the object) and false
positives. What settles it is a `SphereCast` using `PlayerInteractor`'s own `reach`,
`aimAssist` and `blockingLayers`, fired from where a player would stand, and then calling
`Interact` on whatever it actually found.

Call `Physics.SyncTransforms()` before casting in edit mode. Colliders created or moved by a
script keep their old physics pose until something steps the simulation, and a cast against
that stale pose reports a clear path through a door that is plainly shut.

## The answer key

Spoilers, on purpose. Anyone changing a clock needs to know what it is holding up.

| Act | Room | What the player does | Answer | Opens |
|---|---|---|---|---|
| I | หน้าร้าน | Take each wall clock down (F), turn it over (E) and wind it (E). Keep the digits of the four whose pendulums were swinging, read in the order their dials point to | **4719** | display case → กุญแจทองเหลือง |
| II | ห้องช่าง | Collect the ten-second glass off the bench. Take each pocket watch down (F), hold the glass up beside it (F) and start both (E). Eleven keep time; one is only a third of the way round when the sand runs out, and still creeping. Its dial reads 4:40 — set the movement in the vice to that | **4:40** | bench drawer → กุญแจห้องนั่งเล่น |
| III | ห้องนั่งเล่น | Pull the chains under each of the three cuckoos and count the bird. Left to right: 5 · none · 2 | **502** | กรงนก (รหัสอยู่ที่ฐาน) → กุญแจห้องนอน |
| IV | ห้องนอน | A photograph on the bare chest shows that chest with four things on it: ถ้วยชา · เชิงเทียน · แจกัน · กระจกส่องหน้า. Find all four round the room (F), turn each over (E) and read the stock ticket on the back: 4 · 6 · 9 · 2. Enter them in the photograph's order, not the room's. That opens the wardrobe. The weights are in its drawer, which was never locked; the pendulum is on the shelf behind the door, with the three-sided prism. Fit both parts and the clock starts | **4692**, then fit two parts | the clock strikes for five seconds and the secret panel behind it opens itself — there is no E press for it |
| V | หลังตู้นาฬิกา | Fit the three star-filled prisms collected on the way through (drawer in room I, bench drawer in room II, wardrobe in room IV) to the gnomon, three-, four- then six-sided. The train lights and throws six points on the screen. Turn each prism and watch what its own does to the pattern — the first turns it, the second slides it, the third opens it out — until the six points sit on the six marks (the chart beside the screen draws the shape they make) and hold there | **2 · 3 · 4** faces round | the way out — game over |

Two traps are load-bearing and must not be "fixed":

- **Act I**: broken clocks still show a digit when cranked. A player who writes down all
  seven gets nonsense. The reel deliberately never consults the movement.
- **Act III**: the middle cuckoo is `Broken`. Its weights rise like the others and it still
  strikes nothing. Zero is a digit, and the box wants three of them. Skipping it
  leaves the player one short.

**Act IV is the parts system played rather than read.** The longcase in the bedroom is
authored `Running` and still stands dead, because its `Pendulum` and its `Weights` — and
the `Mainspring` that lives with them — start switched off. `ClockPartSocket` turns them on
when the matching item is used on it, and `ChimeRelease` on the wall watches
`ClockMovement.IsRunning`. One part alone is not enough: the pendulum without the weights is
an escapement with nothing to regulate.

**The wall is not a door, and E never opens it.** Fit the second part, the movement starts,
the case strikes for five seconds, and the wall swings open on its own. Two components carry
that and each does one thing:

- **`SealedLock`** is the sixth kind of catch, and the only one that is never put to the
  player. Every other lock is a question they answer — a key, a code, a time, a pattern of
  stars — and this one is answered from the far side. It refuses every press and says so, so
  the wall is still a thing the crosshair finds and still points at the clock.
- **`ChimeRelease`** watches the movement, rings, and calls `Openable.SetOpen` when the
  ringing is done. That method moves a thing regardless of its locks, which is how a lock
  springs its own catch — nothing new had to be asked of `Openable` to get a wall that opens
  itself.

Three things in it are load-bearing:

- **The bell and the wait are one number.** The wall opens when the ringing is done, so the
  sound and the delay have nowhere to drift apart. The rest of the clip is left to ring out;
  a clock does not stop striking because a wall moved.
- **Both the ringing and the release latch.** A spring running down an hour later must not
  take the wall back, and a player who shuts the wall behind them must not have it shoved
  open again on the next frame.
- **`SealedLock` stands down once the thing has opened**, which is what keeps that player
  from being sealed in: after the mechanism has done its work the wall is a door like any
  other, and E shuts and opens it.

`RunningLock` is hung on nothing now. It is still what the seam tests use to prove two locks
of different kinds can sit on one `Openable`, and it is still the right component for a catch
that is *meant* to be opened by hand once a clock is going — this wall simply is not one.

**11:57 runs through the whole house.** One clock in every room is stopped at it — the
Vienna decoy in the showroom, the carriage clock on the bench, `Mantel_2` on the
mantelpiece, and his own wristwatch by the bed. Nothing points at them. The walls of the
last room say it outright, and that is when it lands.

Both settable clocks have **two knobs**, hours and minutes. One knob stepping five minutes
meant seventy-nine presses to cross the dial. The minute knob steps 5, so a target must sit
on the same five-minute grid as wherever that clock stopped: the great movement starts at
**8:02** precisely so that :57 is reachable.

`TimeLock` latches in its `IsLocked` getter rather than in `Update`, so the catch does not
depend on a frame having run since the hands moved.

## Modularity

`MODULARITY.md` is the write-up: what the architecture claims, and the three cases from this
project's own history that test it. The short version, because it governs every choice here:

**A display attached to an object turns that object into a clock. Whether it *goes* is a
separate question, answered by the parts attached beside it. The kind of clock is the sum of
its parts, never a subclass.**

Twelve interfaces carry it — `IClockDisplay`, `IClockPart`, `IPowerStore`, `IInteractable`,
`IInspectable`, `IItemReceiver`, `ILock`, `ICodeInput`, `IActor`, `IReadable`,
`IInHandOnly`, `ITimedTest` — with twenty-eight implementations between them. There are **no abstract classes and no
inheritance**: every one of the fifty-one MonoBehaviours derives straight from
`MonoBehaviour`. Adding a kind of
thing the game has never seen should mean one new file and no edits; if it means editing an
existing class, the seam is in the wrong place.

**Dependencies run one way.** Measured over real code, ignoring mentions in comments:

```
Items        →  (nothing)
Interaction  →  Items
Clock        →  Items, Interaction
Puzzle       →  Interaction
Player       →  Items, Interaction, UI
UI           →  Items, Interaction, Player
Game         →  Interaction, Player
World        →  (nothing)
```

`Clock` and `Puzzle` know nothing about `Player`. The one loop left is `Player ⇄ UI`, and it
is deliberate: those two ship as one assembly, because a HUD that may not name the mode
controller it belongs to is a seam with nothing on the other side of it.

**That table is a compile error now, not a convention.** Five `.asmdef` files —
`EscapeRoom.Items`, `.Interaction`, `.Clock`, `.Puzzle`, `.World` — carry the lower half of
the house. `Player`, `UI`, `Game` and `Editor` stay in the predefined `Assembly-CSharp`,
which is the whole trick: a predefined assembly may reference an asmdef, and an asmdef may
never reference a predefined one. Writing `GetComponent<PlayerMovement>()` inside
`ClockDial` no longer compiles.

Two reasons it is split that way rather than six ways. `PlayerControls.cs` sits at
`Assets/`, not under `Scripts/`, so a `Player` assembly could not see it without moving the
input asset; and `Player ⇄ UI` is a real cycle, which assemblies forbid outright. Leaving
the top layer in `Assembly-CSharp` sidesteps both and proves exactly the same thing.

**A kind of clock is an asset, not a method.** `ClockRecipe` is a case prefab plus a list of
things bolted to it, and `ClockAssembler` turns one into a clock prefab
(`Tools > Escape Room > Clocks > Build Clock From Recipe`). The important thing about the
recipe is what it does *not* have: no separate list for displays and parts. Both are just
"something you screw on", and `ClockMovement` works out which is which by asking what
interfaces they implement.

`PlankClock` is the proof — a board that became a clock because a dial was screwed to it,
built from one data file with no C# method behind it. It keeps the recipe's time, starts
dead because the recipe says `Stopped`, runs when its store is filled, and stops when the
pendulum is switched off, exactly like the eight kinds that were written in code.

`Pendulum` finds its movement with `GetComponentInParent` rather than holding a reference,
which is what let it become a prefab a recipe can list. Anything a recipe fits has to work
by being bolted on, with nobody dragging a wire in the Inspector afterwards.

`Assets/Tests/PlayMode/` holds fifteen tests that run the seams with no scene and no player:
a display class written after the movement still gets the time, a movement with no parts
runs forever, an unfitted part still vetoes, and a plain C# `IActor` — twelve lines, not a
MonoBehaviour — opens locks and fits clock parts. Adding a lock or a display without
breaking those is the bar for a change to this architecture.

Two rules that follow, and that are easy to break by accident:

- **Name an interface for the role, not for the first thing that implemented it.** An
  interface named after a mechanism only ever accepts that mechanism.
- **Prefer finding a collaborator to wiring one.** A serialized reference is a decision
  taken once, in the Inspector, that nobody can see from the code; discovery by role means
  bolting a part on is the whole of fitting it.

## Surfaces and dressing

Everything the player sees that is not shell, clock or puzzle is generated, from two places:

- `TextureKit` draws base maps into `Assets/Textures` — plaster, floorboards, carpet, wood
  grain, brick, stone, woven cloth, the night sky beyond the windows, the sepia prints in
  the frames, and the crosshair. Every map is drawn **white**, and the colour comes from the
  material, so one drawing of oak serves every shade of timber in the house and a colour
  change stays a one-line edit. Its noise wraps on a period, so the tiling maps tile.
- `DressingBuilder` furnishes the rooms. It rebuilds one `Dressing` group per room and
  touches nothing else, the same bargain `HouseBuilder` makes with `Shell`.
  `Tools > Escape Room > House > Dress All Rooms`.

**A cube UVs each face 0..1**, so a two-metre wall and a ten-centimetre drawer front wear
the same single tile: one stretched into wallpaper, the other squeezed into noise.
`BuildKit.Tile` fixes that by scaling the map to the object's world size, and it takes the
UV **offset from the object's world position** as well — without that, two slabs meeting in
a wall each start the texture again and draw a seam down the join.

**The newspaper on the showroom table is not dressing, and that is deliberate.**
`NewspaperBuilder` puts it under the room rather than under `Dressing`, the same way the
sandglass sits under the workshop, because `Dress All Rooms` throws its group away and
rebuilds it — and this is a prop somebody writes on. `Tools > Escape Room > House > Build
Newspaper`.

It splits its words the way the shop's leaflet does. The masthead, the dateline and the
headline are live `Text` on a world-space canvas; the three columns under them are drawn into
`Newsprint.png` as the *shape* of type, because a column of six point news set on a prop is a
grey smudge however it is done. What the columns say is on the `WrittenPage`, and the HUD
reads it out when the paper is lifted.

**Rebuilding it does not eat what was written on it.** All five strings — the three printed
lines and both halves of the `WrittenPage` — are lifted off the old instance before it is
destroyed and put back on the new one, so the geometry can be rebuilt as often as it likes
and the writing survives. A builder that regenerates a prop somebody authors into has to do
this or nobody will ever dare run it twice.

Two things it had to be told, and both are worth knowing before drawing on any flat prop:

- **A cube UVs its top face with u towards −x and v towards −z** — half a turn from the frame
  a canvas laid on the same face is in. The canvas is the one that cannot move, because the
  masthead has to be at +z for `Inspectable` to bring it up the right way round in the hand,
  so the map is authored turned round instead: the sheet's top left is the map's first row,
  last column. Opened as a file the page stands on its head. It is a rotation and not a
  mirror, so a picture drawn into it still comes out the right way round.
- **Which way it lies on the table and which way it reads in the hand are separate questions.**
  `InspectView` turns a held prop to `facing` and forgets the pose it was found in, so the
  paper is laid on the table with its masthead pointing *away* from the room — the end a
  reader stands at — and still comes up the right way round when it is picked up.

`DressingBuilder` also does two things it does not build, because they are puzzle props
that were standing in the wrong place:

- `SeatMovementInVice` takes act two's movement off the post it stood on in the middle of
  the floor and clamps it in a vice on the bench. Deleting the post took the only colliders
  on the prop with it, so a hull goes back on: a clock nobody can put the crosshair on is a
  clock nobody can set.
- `ArrangeAlarms` stands all twelve alarm clocks on the shelf, six to a board, and puts none
  of them on the chest, the bedside table or the bed. Those surfaces belong to act four now:
  the chest is the table in Arthur's photograph and has to be found bare, and a clock left
  standing on it is one more thing the player has to rule out. It leaves the left end of the
  lower board clear for the vase.

**A room that grows has to be lit again.** Room V went from a cupboard to nine metres and its
light did not follow: bulb, crate and candles all stayed by the door, and the machine and the
screen — the whole of act five — stood at the dark end of it. `Behind` puts three wrought
candle stands down there. Not lamps: nothing in this room is wired, and one moon is not enough
light to read a constellation by.

**The sky is night** (`Assets/Materials/NightSky.mat`, procedural with the sun disk off). It
is what the dome is for. Every window in the house is backed by its own painted plane, so the
only places the skybox is ever seen are that dome and the open exit door — both of which the
fiction says are looking at midnight.

**Fire and candles use `Flicker`**, which rides on whatever intensity the light was authored
with rather than setting one of its own, and puts it back in `OnDisable`.

**Only four lights cast shadows.** A point light needs six shadow maps, and eight of them
wanted 48 — URP quietly halves the resolution of every map to make them fit the 4096
atlas, and the line in the console is the only sign it happened. The lights that make the
shape of a room keep their shadows; the secondary ones do not. Four is still 24 maps and
still gets reduced once, to 512, which indoors is fine: 4096 is the largest atlas URP
offers, so the only way to silence it outright is two shadow-casters in the whole house.

`AdditionalLightsPerObjectLimit` is 8, not the default 4. Room V has four candles, a bulb
and the parlour fire within range of the same wall, and at four the fifth light on any one
surface simply stops existing.

## Sound

One sound in the whole house: `Assets/Sound/clock-bg.mp3`, sixty-eight seconds of a room full
of clocks, looping on `Player/Ambience`. There is no other audio, and no script — an
`AudioSource` already does every part of this, and a component that only copied its fields
across in `Awake` would be ceremony.

**It is 2D, and that is the point.** `spatialBlend` is 0, so it is the same in every room and
at every step. A shop with forty-seven clocks in it is heard as one room tone, not as
forty-seven point sources: the 3D version is forty-seven voices, a mix that swims every time
the player turns their head, and a hallway that goes quiet because the nearest clock is behind
a wall. What the player should feel is that the ticking never stops, anywhere.

**It hangs off the player rather than off the house.** At `spatialBlend` 0 the position makes
no odds to the mix, but the listener is on `Main Camera` and the thing the sound belongs to is
the person carrying it. It is a **child** of `Player`, not a tenth component on the root:
`PlayerModeController` switches three components on that root off and on, and an ambience that
lives beside them is one refactor away from being switched off with them.

- **`playOnAwake`, and nothing ever stops it** — including under the title card, which is
  where the mood wants setting. `Frozen` freezes the player, not the house.
- **`preloadAudioData` is on, and the import default is off.** Off, the clip loads on the
  first `Play` and the shop opens on silence for a moment. `loadInBackground` is off with it,
  and the load type stays `DecompressOnLoad`: a few megabytes against any chance of a decode
  hitch at the loop point, which is the one artefact a bed like this cannot have.
- **The clip's own level is the level.** It peaks at −5 dBFS and sits around −39 RMS, which is
  a *sparse* bed and not a quiet one — single ticks are loud, and the space between them is
  what makes the average low. The source is already at 1.0, so louder is an `AudioMixer`
  group, never a bigger number in that field.
- **The loop seam is 190 ms of silence and does not show.** An mp3 carries encoder padding:
  152 ms at the head of this one and 38 ms at the tail, which meet at the loop. Measured
  against the bed itself — 1.67 ticks a second, gaps between them already running to 730 ms —
  the seam lands inside the range the ear is hearing anyway. Measure before trimming anything.
- **One `AudioListener`, on `Main Camera`.** A second anywhere and Unity picks one of them and
  says so once, quietly.

**The second sound is the striking**, on `SecretPanel/LongcaseClock/Bell`:
`Assets/Sound/clock-bell.mp3`, `playOnAwake` off, played once by `ChimeRelease` when the
longcase comes back to life. It is 2D as well — a cue that opens the way out is not one the
player may be standing in the wrong place for — and it hangs on the clock rather than the
wall because it is the clock's bell, so switching it to 3D one day is a single field and no
moving.

Two things measured off that clip, so nobody has to guess at them again:

- **It opens on a soft lead-in.** The first 1.5 s is a mechanism gathering itself (rms
  0.008), and the striking proper runs from there to about 6 s (rms 0.035, peaks 0.17). Five
  seconds of ringing is therefore a wind-up and three and a half seconds of bell, which is
  the shape the wall wants — not five seconds of dead air.
- **It is quiet in absolute terms and loud in the only terms that matter.** It peaks at
  −15 dBFS where the ambience peaks at −5, but its rms while striking is three times the
  ambience's, so it stands well clear of the ticking underneath it. Both sources sit at 1.0,
  and anything louder is an `AudioMixer` group rather than a bigger number.

**The third is the bench test**, `clock-ticking-down.mp3` on an `AudioSource` beside `TestRun`
on each of the twelve pocket watches, played by `RunTicking` for exactly as long as a run
lasts. 2D again, and `playOnAwake` off — twelve watches carrying a clip is twelve watches
ticking the moment the scene loads if that one field is left alone.

It is a steady bed at a constant level (rms 0.0115, peaks 0.34) with no build to it, so
looping a piece of it loses nothing; the ticks are on a 0.255 s grid, which is what makes 5.1
the right length to repeat and 5.0 the wrong one. See **Puzzle 2** for why the piece is
repeated at all.

**The fourth is the cuckoo**, `clock-cuckoo.mp3` on an AudioSource on each cuckoo clock,
played straight through by `ClockStriker` and stopped when the count is done. The recording
is a bird calling eleven times over — first at 1.035 s, then every 0.805, each call a loud
note with a softer one 0.245 s behind it — and those numbers are the clock: the door opens
and shuts against the playhead rather than against a stopwatch of its own. See **Clocks**.

Every sound in the house is on one of these four, all 2D, all at volume 1.0. Nothing is mixed
against anything, because there is nothing to mix: the bed sits at −39 dBFS rms and the three
that matter all sit well over it — the cuckoo loudest of the lot, which is right for the one
the player has to count.

## Scene conventions

- Floor is `y = 0`. `Ground` sits at −0.5 because it is a scaled cube.
- `Player` is the mover: `CharacterController` h 1.8, r 0.3, center (0, 0.9, 0).
- `CameraPivot` at local y 1.6 takes pitch. Yaw goes on `Player` itself — pitching the
  body would tip the capsule.
- `Main Camera` is offset to local z 0.22, ahead of the chest, with near clip 0.035 and far
  clip 80. The defaults slice through the player's own body at one end and spend the whole
  depth buffer on empty space at the other; the house is sixteen metres across.

  **The eye must stay inside the capsule.** `CameraPivot` had drifted to local z 0.10, which
  put the eye 0.32 forward of a capsule of radius 0.30 — the near plane then reached 0.37,
  and walking into a wall let the player see through it into the next room. The pivot's z is
  0 for that reason, and any change to the pivot offset, the camera offset, the near clip or
  the controller radius has to keep `eye + nearPlaneCorner < radius`.
- `MouseLook` clamps look-down to 60°, not 85°. Past that the player's chest fills the
  screen. Up and down limits are separate fields for this reason.
- `Character` is scaled 1.10 so the 1.63 m model matches the 1.8 m controller.
- **A head bob is set by its cadence, not by its stride.** What the player feels is
  `walkSpeed × stepsPerMetre` — footfalls a second — and the amplitude only decides how far
  each one throws the picture. `stepsPerMetre` was 1.55, a true 65 cm stride, which at
  2.5 m/s is 233 footfalls a minute: a sprint, and it read as a shudder rather than as a
  walk. It is 0.85 now, which is 128 a minute, with the dip down from 30 mm to 18 and the
  swing from 40 to 24. The stride that implies is unrealistically long and nobody will ever
  measure it; the rhythm is the part that is felt.
- `HeadBob` rides on `CameraPivot`'s **position**, which `MouseLook` never touches — it only
  ever writes that pivot's rotation. It reads the controller's own velocity rather than the
  input, so walking into a wall stops the bob without anything having to tell it.

  **A `CharacterController` reports the last `Move` it was given, for ever.** It does not
  decay to zero when nobody moves it: switch `PlayerMovement` off mid-stride — F on a prop,
  or the bag — and `velocity` keeps answering 2.5 m/s while the player stands perfectly
  still, so the eyes bobbed on with nothing driving them. That was the "walk animation never
  stops" bug, and it was the *camera*, not the legs. Both readers now check the mover
  itself: `HeadBob` takes velocity as zero while `PlayerMovement` is disabled, and
  `PlayerAnimation` sets `Walking` from `enabled && IsMoving`.

  The rule behind it: **anything that keeps running while the player is switched off must
  ask whether the thing it reads is still being driven.** `PlayerModeController` disables
  three components; every other component on the player carries on, and a value read out of
  a stopped system is stale rather than absent — which is worse, because it looks alive.
- `PlayerModeController.startingMode` is `Frozen`, because the game opens on a title card.
  `TitleScreen.Begin` is the one thing that ends that, and it hands control back through
  `SwitchTo` rather than reaching for `MouseLook` or the cursor itself.

## Working with the Editor

Unity is usually open, reachable over MCP at `http://127.0.0.1:8080/mcp` (declared in
`.mcp.json`). After editing asset files on disk, call `refresh_unity` — the Editor does
not notice external changes until it refreshes or regains focus.

Never edit `.inputactions` on disk while its Input Actions window is open in Unity. That
window holds its own copy in memory and writes over external changes.

Prefer making scene changes through the Editor via MCP rather than editing `.unity` files,
and wrap them in `Undo` so a single Ctrl+Z reverts the batch.

`Run In Background` is on in Player Settings. Without it play mode freezes the moment the
Editor loses focus, which looks exactly like a bug in whatever was being tested — stopped
hands, colliders that never come back — and is not.

**Never render through the player's own camera.** Screenshots go through a throwaway camera
created, used and destroyed inside the same call. Borrowing `Main Camera` for one shot
leaves it wherever `LookAt` put it, and in edit mode that gets saved into the scene — the
game then starts with its view metres away from the player, aimed at nothing. Putting it
back means `CameraPivot` at local `(0, 1.6, 0)`, `Main Camera` at local `(0, 0, 0.22)` with
identity rotation and near clip 0.035, and `InspectCamera` at local zero.

`GameObject.Find("Name")` with no path matches that name **anywhere** in the scene, so two
calls meaning two different objects can quietly return the same one. Take objects by path,
or from `scene.GetRootGameObjects()`.

Textures whose size is not a power of two are shrunk to the nearest one on import unless
`npotScale` is set to `None`. `NotePage` (512×704) and `ClockDigits` (96×960) both need it.

A texture imported as a Sprite defaults to `spriteImportMode = Multiple`, and a Multiple
sprite has no single sprite in it: `LoadAssetAtPath<Sprite>` hands back null and whatever
wanted it — the crosshair — stays a bare white box. `TextureKit.Save` pins it to `Single`.

A bare Unity `Quad` faces into its parent, which leaves it invisible sunk in a recess. Half
a turn about Y fixes it; that is a rotation, not a mirror, so the digits still read the
right way round.

**`EditorUtility.CopySerialized` onto a mesh asset updates the file and not the mesh.** The
copy lands on disk, but the `Mesh` already loaded goes on holding the buffers it had, so
everything pointing at it draws whatever the asset used to be — or, if the asset is new to
this session, nothing at all. The rose spent an afternoon as a renderer reporting `isVisible`,
a valid submesh, seventeen vertices, twenty-four indices, and not one pixel; the same geometry
built in memory drew first time. Write mesh assets out fresh — delete and create — unless
something is holding a reference the delete would break.

**Legacy `Text` on a dynamic font stops growing at `fontSize` 500.** Ask for 900 and the
glyphs come out at 500, with nothing in the console. On a world-space canvas that means the
size of a word is set by the units the canvas is drawn in and not by the number typed into the
field: the shop's leaflet is ten units to the millimetre and never meets this, because its
heading is 190, but the doormat wants a word 60 mm tall and is drawn at four. If type will not
grow, change the scale, not the number.
