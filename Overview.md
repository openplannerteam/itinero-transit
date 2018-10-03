Overview
========

This document aims to give an overview to new contributors. It aims to give pointers to get to know the code structure.

First, start with reading the paper 'Connection Scan Algorithm', by Julian Dibbelt, Thomas Pajor et al. This will give you an idea of the used algorithms and terminology.

Second, the library works with linked connections, so every object is identified with a URI. If you have never heard about linked data and the semantic web (or simply need a refresher) have a look at [Ruben Verborgh's slides](http://rubenverborgh.github.io/WebFundamentals/semantic-web/).

Major classes and interfaces
----------------------------

### LinkedObject

Nearly everything extends LinkedObject. Apart from containing an URI that identiefies the object, it has some helper functions to download stuff.

### IConnection and ITimeTable

The most important interface of this codebase is 'IConnection'. It contains the contract of what properties every single connection has. (A connection is one piece that a train/bus/... travels from one location to another).

The most important properties of a connection are when and where it departs and arrives.

For efficiency, connections won't be downloaded from the Public Transport Operator one by one, but in batches of around 100 (up to 200) connections. These are grouped in a ITimeTable. The timetable keeps track of the connections themself, what time the timetable is valid (e.g. 10:00 till 10:16 on oct 10th) and of the next and previous timetable.

### Journey

The goal of this library is to chain multiple connection together, resulting in a Journey. A journey represents all the pieces the traveller must follow in order to reach his final destination.

A Journey-object consists of the last connection taken in the journey and a backlink to the previous journey. This way, multiple journeys can branch of one common 'ancestor' journey. Following the backlinks will construct the entire travel.

At last, every Journey contains an object of type 'IJourneyStats' (which is actually passed as type variable to the class)

### IJourneyStats and IStatsComparator

A journey-statistic object keeps track of some statistics of the journey. The exact specifics can be chosen by the implemention, depending on what is needed and what should be optimized. (e.g. if the user cares very much about the number of transfers but not about total travel time, only number of transfers should be kept)

In order to compare statistics, IStatsComparator should be implemented. This offers the possibility to optimize the journey to your own preferences.

Have a look at a few examples in Stats/

### IConnectionProvider

Before we can start creating journeys, we need a source of data. These are represented by 'IConnectionProvider', which is an interface representing a source of connections. The main entry point is to get a certain timetable through the URI of the needed timetable.

The IConnectionProvider offers an opportunity to cache. You can save the timetables to local storage with a `LocallyCachedProvider` which will store connections locally.

Algorithms
----------


With all the preliminary requirements in place, we can finally implement our algorithms.

For now, the following algorithms are supported:

 - EarliestConnectionScan: given a startlocation and time, what is the earliest time a traveller can arrive in a destination station
 - ParetoFrontier: given a list of Journeys, are they pareto-optimal with respect to each other? This class is mainly used in other algorithms
 - ProfiledConnectionScan: given a departure- and arrival-location, what are all pareto-optimal routes in a given timespan?















