Overview
========

This document aims to give an overview to new contributors. It aims to give pointers to get to know the code structure.

First, start with reading the paper 'Connection Scan Algorithm', by Julian Dibbelt, Thomas Pajor et al. This will give you an idea of the used algorithms and terminology.

Second, the library works with linked connections, so every object is identified with a URI. If you have never heard about linked data and the semantic web (or simply need a refresher) have a look at [Ruben Verborgh's slides](http://rubenverborgh.github.io/WebFundamentals/semantic-web/).

Thirdly, most classes (should) have a banner comment stating the purpose of the class and where it fits in the whole. However, it helps to have an succint overview to start and to find your way around the code. You'll find this overview below.

Major classes and interfaces
----------------------------

### LinkedObject

Nearly everything extends LinkedObject. Apart from containing an URI that identiefies the object, it has some helper functions to download stuff.

### IConnection and ITimeTable

The most important interface of this codebase is 'IConnection'. It contains the contract of what properties every single connection has. (A connection is one piece that a train/bus/... travels from one location to another).

The most important properties of a connection are when and where it departs and arrives.

For efficiency, connections won't be downloaded from the Public Transport Operator one by one, but in batches of around 100 (up to 200) connections. These are grouped in a ITimeTable. The timetable keeps track of the connections themself, what time the timetable is valid (e.g. 10:00 till 10:16 on oct 10th) and of the next and previous timetable.

Walks between stops are represented as Connections too. However, as walks and transfers can be moved in time freely, they implement the interface `IContinuousConnection`. 

### Journey

The goal of this library is to chain multiple connection together, resulting in a Journey. A journey represents all the pieces the traveller must follow in order to reach his final destination.

A Journey-object consists of the last connection taken in the journey and a backlink to the previous journey. This way, multiple journeys can branch of one common 'ancestor' journey. Following the backlinks will construct the entire travel.

At last, every Journey contains an object of type 'IJourneyStats' (which is actually passed as type variable to the class)

### IJourneyStats and IStatsComparator

A journey-statistic object keeps track of some statistics of the journey. The exact specifics can be chosen by the implemention, depending on what is needed and what should be optimized. (e.g. if the user cares very much about the number of transfers but not about total travel time, only number of transfers should be kept)

In order to compare statistics, IStatsComparator should be implemented. This offers the possibility to optimize the journey to your own preferences.

Have a look at a few examples in `Stats/`

### IConnectionProvider

Before we can start creating journeys, we need a source of data. These are represented by 'IConnectionProvider', which is an interface representing a source of connections. The main entry point is to get a certain timetable through the URI of the needed timetable.

The IConnectionProvider offers an opportunity to cache. You can save the timetables to local storage with a `LocallyCachedProvider` which will store connections locally.

In the directory `ConnectionProviders` you can find some implementations:

- `LinkedConnectionProvider` is the connection provider which loads data remotely from a Linked Connections server
- `LocallyCachedProvider` is a wrapper class around another IConnectionProvider. All requested timetables will be retrieved from disk, and only downloaded if not found yet.
- The `ConnectionProviderMerger` merges timetables from multiple sources, to make intermodal transfers possible.

Furthermore, you'll find a few useful extension methods in `ConnectionProviderExtensions`.

### ILocationProvider

An ILocationProvider is responsible for the conversion of location-URI's into coordinates and is responsible in finding locations nearby a certain coordinate (to search possible intermodal transfers).
   


### Profile

A profile bundles a ConnectionProvider, LocationProvider and Transfergenerator, along with some more settings.

It represents the profile of the traveller: which PT operators are allowed? How long do transfers take? How fast does the traveller walk?

The profile is then passed in the core algorithms, together with the departure- and targetlocations; and desired times.

Algorithms
----------


With all the preliminary requirements in place, we can finally implement our algorithms.

For now, the following algorithms are supported:

 - EarliestConnectionScan: given a startlocation and time, what is the earliest time a traveller can arrive in a destination station
 - ParetoFrontier: given a list of Journeys, are they pareto-optimal with respect to each other? This class is mainly used in other algorithms
 - ProfiledConnectionScan: given a departure- and arrival-location, what are all time-optimal routes in a given timespan?

The inner workings of the algorithm are stated in the classes themselves.



Usage
-----

# Intramodal routing from station to station

1) Start with creating a profile. There are two Belgian PT-operators available by default: Delijn and NMBS/SNCB.
(**Note: this is still subject to change**)

        // A downloader is needed first
        var loader = new Downloader();
        // And a place to store the locally cached files
        var storage = new LocalStorage("cache_directory");
        // At last, to calculate interstop foothpaths, a routerdb is needed
        var routerDb = "belgium.routerdb";

        var profile = Sncb.Profile(loader, storage, routerdb)

2) Time to decide when and where you leave and whish to arrive. In this case, the identifiers of the PT provider are used:

        public static Uri Poperinge = new Uri("http://irail.be/stations/NMBS/008896735");
        public static Uri Vielsalm = new Uri("http://irail.be/stations/NMBS/008845146");

        var startTime = new DateTime(2018, 10, 17, 10, 8, 00);
        var endTime = new DateTime(2018, 10, 17, 23, 0, 0);


3) What if the traveller was to leave at the starttime precisely, what is the earliest time that they could arrive? This includes not caring about other factors, such as the number of transfers. 

The endTime is still given with the algorithm: if no suitable journey can be found that arrives before the specified endTime, the algorithm will crash with an exception. This is to prevent infinite loops when no suitable route can be found.

For this, EarliestArrivalScan is used: 

        // Initialize the algorimth
        var eas = new EarliestConnectionScan<TransferStats>(
            Poperinge, Vielsalm, startTime, endTime, 
            profile);
        var journey = csa.CalculateJourney();
        // Print the journey. Passing the profile means that human-unfriendly IDs can be replaced with names (e.g. 'Vielsalm' instead of 'https://irail.be/stations/12345')
        Log.Information(journey.ToString(profile)))

3) When interested in possible journeys during a certain period, use a ProfiledConnectionScan:

        var pcs = new ProfiledConnectionScan<TransferStats>(
                Poperinge, Vielsalm,
                startTime, endTime, profile);
        var journeys = pcs.CalculateJourneys()[Poperinge];

Note that a dictionary mapping the 'departure station' onto possible journeys is returned.

This is because PCS supports multiple departure points:

        var pcs = new ProfiledConnectionScan<TransferStats>(
                new List<...>{Poperinge, Brugge}, Vielsalm,
                startTime, endTime, profile); 

Or even with walks in or out:

  
            var home = new Uri("https://www.openstreetmap.org/#map=19/51.21576/3.22048");
            var startLocation = OsmLocationMapping.Singleton.GetCoordinateFor(home);
            var starts = deLijn.WalkToClosebyStops(startTime, startLocation, 1000);

            var station = new Uri("https://www.openstreetmap.org/#map=18/51.19738/3.21830");
            var endLocation = OsmLocationMapping.Singleton.GetCoordinateFor(station);
            var ends = deLijn.WalkFromClosebyStops(endTime, endLocation, 1000);


            var pcs = new ProfiledConnectionScan<TransferStats>(
                starts, ends, startTime, endTime, deLijn);


Running tests
-------------

The first time you'll run the tests, most will fail. This is because

1) The belgium routerdb isn't there yet. This database is used for foothpath navigation and automatically download by the test 'FixRouterDB' in  ResourcesTest. This test will download ~250mb and process the file afterwards; which can take quite some time (~15minutes, till over 1h on older hardware. Luckily, this should only be done once
2) The local testdata should be copied too to the appropriate locations









