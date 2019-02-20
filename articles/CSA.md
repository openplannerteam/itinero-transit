 Connection Scan Algorithm: practical expansions
 ===============================================
 
 The core principle of every connection scan is to have a look at every connection, ordered by departure time. 

 There are a few variations that each give useful results for some usecases:
 
- Earliest Arrival Scan (EAS)
- Latest Arrival Scan (LAS)
- Profile Connection Scan (PCS)
- Extension with trips
- Extension with walks

We will give a small overview of these algorithms here. A reader interested in all the details, is refered to [the paper](CSA.pdf).


A tree of journeys
------------------

When running the algorithm as in the [example](index.md#an-example-execution-of-csa), the traveller only kept track of when he could arrive in at certain stop.

Of course, it is way more useful to also know how he could end up at that location and keep track of the way travelled.
For this, we reuse the datastructure of [journeys consisting of multiple parts](index.md#a-journey). Recall that, when having a journey part, we could reconstruct the journey by following the pointers to the `previous`-journey part. However, there is nothing stopping us from having multiple parts pointing to a common previous journey.

### Building the journey tree

For example, take the following timetable:

        A, 10:00 --> B, 10:30 
        B, 10:35 --> Y, 11:00
        B, 10:40 --> X, 11:10
        
If the traveller, departing at `A` wanted to calculate all the earliest arrival times to `X` and `Y`, he would start again with building an empty table:

        A: ?
        B: ?
        X: ?
        Y: ?
        
Again, he would mark his departure stop and time - this time however as a journey part representing the start of his journey (a _genesis_ journey part):

        A: (Journey part nr 0: Genesis in A at 10:00)
        B: ?
        X: ?
        Y: ?
        
When scanning the first connection, he deducts he can get to B. Instead of only keeping track of the arrival time, the connection taken and _a pointer to_ the previous link is kept in a journey part:

        A: (Journey part nr 0: Genesis in A at 10:00)
        B: (Journey part nr 1: Take connection A,10:00 --> B, 10:30; previous journey part is 0)
        X: ?
        Y: ?    

The same is done for the next connection:

        A: (Journey part nr 0: Genesis in A at 10:00)
        B: (Journey part nr 1: Take connection A, 10:00 --> B, 10:30; previous journey is 0)
        X: ?
        Y: (Journey part nr 2: Take connection B, 10:35 --> Y, 11:00; previous journey is 1)

And analogously the last connection:

        A: (Journey part nr 0: Genesis in A at 10:00)
        B: (Journey part nr 1: Take connection A, 10:00 --> B, 10:30; previous journey is 0)
        X: (Journey part nr 3: Take connection B, 10:40 --> X, 11:10; previous journey is 1) 
        Y: (Journey part nr 2: Take connection B, 10:35 --> Y, 11:00; previous journey is 1) 

### Reconstructing a journey

If the traveller wants to know how he could end up at `Y`, he can simply follow the breadcrumbs. He start by the journey part in Y and noting the connection:

        Take connection B, 10:35 --> Y, 11:00

Then, he follows the pointer to connection nr 1, and writes the contained connection:

        Take connection A, 10:00 --> B, 10:30
        
He follows the next pointer and takes the connection:

        Take connection A, 10:00 --> B, 10:30
        
The last pointer is a genesis:

        Start in A at 10:00
        
When reading these instructions from bottom to top, he knows what trains to take to get to `Y`!

### Tree structure

Note that the journey parts to end up in both `X` and `Y` point to the _same_ common ancestor, namely connection part nr 1. In other words, all those journeys together form a tree, with the Genesis as root and every destination as leaf:


              Genesis in A
                   |
                   |
                B,10:30
                /     \
               /       \
              /         \
          X, 11:10      Y, 11:00


Earliest Arrival Scan
---------------------


*Earliest arrival scan* (EAS) is the simplest form of connection scan algorithm and is exactly performed exactly as [in the example on core conecpts](index.md#connection-scan-algorithm) with the single addition of [journeys as tree](#a-tree-of-journeys) as above.

EAS is thus characterized by:

- Needing a clear, known _*departure* time and location_.
- Keeping track of an _earliest arrival table_ which keeps track of the earliest arrival from his departure
- Enumerating all the connections by departure time from earliest to latest connection.

EAS is used in the following use cases:

- If the traveller wants to know at what earliest time he could arrive in a given location
- If the traveller wants to know at what earliest time he could arrive in one of a given set of locations
- If the traveller wants to known what locations he could reach by a given time (thus calculating an isochroneline)


Latest Arrival Scan
-------------------


*Latest arrival scan* (LAS) is the inverted sibling of connection scan. It works just like EAS, but the connections are scanned backwards.
Where EAS focuses on departure location, LAS focuses on _arrival location_. 

LAS is thus characterized by:

- Needing a clear, known _*arrival* time and location_
- Keeping track of a _latest departure table_ with journeys which'll bring the traveller (just in time) to his destination
- Enumerating all the connections by departure time, from *latest* to earliest connection.

LAS is used in the following use cases:

- If the traveller wants to know at what _latest_ time he could depart at his location to arrive in a given location
- If the traveller wants to know at what latest time he could depart in one of a given set of locations
- If the traveller wants to known what locations he could depart at a given time to end up in the arrival location (thus calculating an isochroneline)

As sidenote, journeys are constructed just in the same way as with EAS - with the difference that the pointer to the rest of the journey now indicates the _next_ step to make (instead of the previous step). In other words, the journeys will now appear in order instead of reversed. The library however conveniently reverses journeys for the end user so that you do not have to think about this.

Profile connection scan
-----------------------

The *Profile Connection Scan* functions in a backwards fashion, just as the [_latest connection scan_](#latest-arrival-scan). The major difference is that the central table does keep track of one profile for _each_ location, instead of a single journey for each location.

### Profile

A *profile* has the following properties:

- The profile is a [pareto front](index.md#pareto-frontiers) of journeys
- The metric for the pareto front includes the user-chosen metrics, e.g. the total travel time, total number of transfers, ...
- The pareto front includes a sense of _covering in time_, so that a journey `j` is only suboptimal with respect to another journey `k` if `j` performs worse on the metric _and_ departs earlier then `k` but arrives later then `k`.

In other words, if the pareto set describing all possible trips from `A` to `B` would contain only the following single journey:

        {
            journey: A: 10:00 -> B: 11:00, transfers: 0
        }

Then, the journey `journey: A: 09:50 -> B: 10:50, transfers: 2` _would_ become part of the profile when it is discovered by the connection scan algorithm.
Although it is worse then the already existing trip (same time but more transfers), it is included: the other journey does not cover it completely.
The profile thus becomes:

        {
            journey: A: 10:00 -> B: 11:00, transfers: 0
            journey: A: 09:50 -> B: 10:50, transfers: 1
        }

### Connection scanning

With the profile in place, we can have a look at the actual connection scan algorithm. Again, every connection is scanned, reverse sorted by departure time.

Again, there is a table keeping track of all profiles:

        A: {journey: A: 10:00 -> B: 11:00, transfers: 0
           ; journey: A: 09:50 -> B: 10:50, transfers: 1}
        B: {journey: arrival at B at 12:00}
        X: ?
        Y: ?
        
When a connection `X, 09:00 --> A, 09:40` is scanned, the profile at the arrival location is taken, here `B`: `{journey: A: 10:00 -> B: 11:00, transfers: 0; journey: A: 09:50 -> B: 10:50, transfers: 2}`. New journeys are constructed based on the connection and the journey, giving a candidate set for `X`:

        {
            journey: X: 09:00 -> B: 11:00, transfers: 1
            journey: X: 09:00 -> B: 10:50, transfers: 1
        }

Now, something peculiar happens: this turns out to be the direct train for the second journey. In other words, the number of transfers becomes the same for both now - and the slightly worse journey departing in `A` becomes the optimal one here - thus, one of the journeys can be removed as being non-optimal, giving the new profile for X:

        A: {journey: A: 10:00 -> B: 11:00, transfers: 0
           ; journey: A: 09:50 -> B: 10:50, transfers: 1}
        B: {journey: arrival at B at 12:00}
        X: {journey: X: 09:00 -> B: 10:50, transfers: 1}
        Y: ?


This gives an idea of how the algorithm more or less works. The explanation above is however not enough for a correct functioning, see 'trips' below.

### Characteristics

In summary, PCS is used when the traveller wants _a range of options_.

Just as in LAS, a clear destination should be given, together with a timewindow in which the travel can be made. The traveller will end with a table with all the possible journey from each possible destination station.
   

Handling trips
--------------

As small sidenote, a journey does _not_ satisfy an important property: if a journey from `A` to `C`, goes via `B` and is the *optimal* journey from `A` to `B` with respect to some metric - this does *not* mean that its subjourneys from `A` to `B` or from `B` to `C` are the optimal ones!

The simplest situation where this arises if if busses `x` and `y` depart at location `A` at the same time. Due to some circumstances, `x` arrives one minute earlier at `B` then `y`; so the optimal route from `A` to `B` would be to take `x`. However, if only bus `y` continues to `C`; bus `y` becomes the optimal choice.

To deal with this, an extra table _trips_ is introduced, which keeps track of all the trips on which a traveller could be. For more details, please see the [paper](CSA.pdf)

Making walking transfers and intermodality
------------------------------------------

Another extension of CSA is _walking_ from one PT-stop to another; In some datasets, a _station_ is not explicitely modeled but rather a group of stops which happen to be close to each other. In this case, the traveller needs to walk from one stop or another.

Conceptually, one can think of a _walk_ as another journey part. Whenever a real vehicle arrives at some stop, a (virtual) 'walk connection' departs to each closeby stop and can be saved in the datastructures.

For more information, we again refer to the [paper](CSA.pdf)  
