 
 Core concepts
 =============

Itinero-Transit calculates journeys over public transport networks.

This document defines a few core concepts and informally describes how the library calculates journeys.

 A connection
 ------------
 
A *connection* is the smallest element in a public transport network. A connection represents a PT-vehicle which travels from one station to the next, _without intermediate stops_.
In most circumstances, the vehicle will continue to a next stop without the passengers having to leave. A chain of such connections is called a *trip*. Knowing the trip of which a connection is part, is important to known how many transfers are needed to make a journey.

A connection is characterized by five data points:

- The departure stop
- The arrival stop
- The time of departure
- The time of arrival
- A trip identifier

In practice, some other data points are saved needed as well (e.g. vehicle type, operator, headsign, ...). They are however unimportant for the core concepts.
 
 A journey
 ---------
 
A *journey* is a list of connections and transfers, representing how a traveller might get from its departure to its arrival.

A journey consist of multiple parts. A part can describe either

- A connection that is taken
- A transfer in a single stop - thus getting of one vehicle and getting on another without walking
- A walk, from one stop to another
- The start of the journey

Each part thus contains

- A value indicating if the journeypart is a connection, a transfer, a walk or the beginning
- The location of the traveller after executing this journey part
- The time it is after executing this part
- A pointer to the previous part (if any)
 
Thus, if one has a journey part which ends in some location, one can construct the entire journey needed to get there by following the pointers pointing to the `previous` part, not unlike Hans and Gretel in the forest.


 Linked Connections as data source
 ---------------------------------
 
This library uses [Linked Connections](https://linkedconnections.org/) as datasource. Linked Connections are a way to format public transport data feeds in a semantically coherent way. The data is organized as pages of around 200 individual connections of a certain timespan (e.g. a quarter of an hour), sorted by departure time.

 TransitDB
 ---------
 
Once the connections are loaded, they are stored into the `TransitDB`. This is an in-memory database of connections, trips and stops.

By having the TransitDB, the library can either cache connections for solving multiple queries or can dynamically reload the needed data needed when a query comes in.
 
 Connection scan algorithm
 -------------------------

The *connection scan algorithm (CSA)* is the core algorithm of this library.

### An example execution of CSA

The essence of the connection scan algorithm can best be shown by running an extremely simple example. 

The input for this example is a small timetable. This timetable is *sorted by departure time*.

       Departure time, departure stop --> arrival time, arrival stop
       10:00, A --> 10:25, C
       10:05, X --> 10:55, Y
       10:10, A --> 10:50, B
       10:15, B --> 10:30, X
       10:30, C --> 10:40, B 
       10:35, C --> 10:45, Y
       10:45, Y --> 11:00, Z
       
This timetable can be reused for every query.

If a traveller, departing in `A` at `10:00` wanted to know the earliest time he could possible arrive at `B`, he can calculate this by performing the following steps:

1. Create a table with all possible stops. This table contains the earliest time that he can arrive at that stop. As no earliest arrival time is known yet, the traveller puts question marks everywhere. We will call this table the _earliest arrival table_.

        A: ?
        B: ?
        C: ?
        X: ?
        Y: ?
        
2. The traveller wants to depart in `A` at `10:00`. This is marked in the table:

        A: 10:00 (changed)
        B: ?
        C: ?
        X: ?
        Y: ?
        
3. Now, the traveller takes the timetable and has a look at all the connections, in order.
   The first connection is `10:00, A --> 10:25, C`. As the _earliest arrival table_ indicates that he can be in `A` at `10:00`, the traveller *could* take this connection and *could* arrie in `C` at `10:25`. This is marked in the table:
   
        A: 10:00
        B: ?
        C: 10:25 (changed)
        X: ?
        Y: ?   

4. The traveller takes a look at the next connection: `10:05, X --> 10:55, Y`. The _earliest arrival table_ has no value yet for `X`, indicating that the traveller could not be in `X` at `10:05`. The connection is ignored.

5. The next connection is scanned: `10:10, A --> 10:50, B`. The traveller could take this connection, as -according to the _earliest arrival table_- he could be in `A` in time. And this connection takes him to his destination! The travellers quickly notes this in the table:

        A: 10:00
        B: 10:50 (changed)
        C: 10:25
        X: ?
        Y: ?

6. As next connection, `10:15, B --> 10:30, X` comes up - which can not be taken and is ignored. 

7. The traveller eagerly scans the next connection: `10:30, C --> 10:40, B`. 
   The _earliest arrival table_ indicates that the traveller can already be in `C` at `10:25`, so catching this connection can't be a problem. It takes us to `B`. The traveller already could reach `B`, but this connection will take him there ten minutes sooner, so the table is changed:
   
        A: 10:00
        B: 10:40 (changed)
        C: 10:25
        X: ?
        Y: ?

8. The traveller dutifully keeps scanning connections. `10:35, C --> 10:45, Y` could be taken and brings him to a new stop:

        A: 10:00
        B: 10:40
        C: 10:25
        X: ?
        Y: 10:45 (changed)

9. The next connection is looked at: `10:45, Y --> 11:00, Z`. The traveller realizes that he wants to go to `B` and, according to the _earliest arrival table_, he could get there by `10:40` already. This connection departs after he already could have arrived, so there is no point in scanning this connection. As the time table is organized by departure time, all subsequent connections will be later as well. In other words, the traveller is done.

10. The traveller can now easily lookup in his table the earliest time he could arrive in `B`: namely at `10:40`. As sideproduct, he also knows at what times he could arrive in the other stations, if departing at `10:00` in `A`.

### Further complications

The above example neatly illustrates the core principle of CSA.
Of course, there is much more work to do: the traveller above knows at what time he'll arrive, but he forgot to keep track which trains he should have taken and how many transfer he has to make. Did he take the direct train or did he transfer in C?

And although knowing when a traveller could arrive as soon as possible is useful, most travellers want to minimize either travel time or number of needed transfers. It is thus useful to offer all these best (in one sense) journeys to the user of the library and to let the user pick.

The technicalities of these algorithms can be found [here](CSA.md). It is not necessary to known those to effectivally use the library.

Pareto Frontiers
----------------

When calculating all journeys (say: every possible journey from `A` to `B`, departing between `10:00` and `12:00`), multiple journeys will be found with different properties. For example, one journey might take only 30 minutes - but needs a transfer halfway whereas the direct train needs 40 minutes. It is difficult for -as library- to make the choice of metric for the end user.

The library copes with this by tracking _both_ metrics by default. If a profile search is needed, _all_ journeys which perform _good_ are kept. In other words, a set of journeys is returned so that each journey in the set performs better on some metric then all the other journeys in the set. Such a set is called a *paretor frontier*.

An example of such a frontier is `{journey with 0 transfers in 1:00, journey with 1 transfers in 0:50, journey with 2 transfers in 0:40}`. The first is superior in the number of transfers, the last is the shortest in travel time. The middle one outperforms the zero-transfer one in time and outperforms the fastest one in number of transfers and might be a good tradeoff for some travellers too. A journey with 2 transfers taking 45 minutes has no place in the pareto frontier, as it is outperformed by the last journey: it takes 5 minutes more and just as much transfers, so it is clearly non-optimal.

For more information on Pareto Frontiers, refer to [Wikipedia](https://en.wikipedia.org/wiki/Pareto_efficiency#Pareto_frontier)
