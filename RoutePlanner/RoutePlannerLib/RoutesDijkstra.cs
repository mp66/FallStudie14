﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fhnw.Ecnf.RoutePlanner.RoutePlannerLib
{
    public class RoutesDijkstra : Routes
    {
        public override event RouteRequestHandler RouteRequestEvent;

        //Konstruktor
        public RoutesDijkstra(Cities cities): base(cities)
        {
        }

        public Task<List<Link>> GoFindShortestRouteBetween(string fromCity, string toCity, TransportModes mode)
        {
            return Task.Run(() => FindShortestRouteBetween(fromCity, toCity, mode));
        }

        public Task<List<Link>> GoFindShortestRouteBetween(string fromCity, string toCity, TransportModes mode, IProgress<string> reportProgress)
        {
            return Task.Run(() => FindShortestRouteBetween(fromCity, toCity, mode, reportProgress));
        }

        #region Lab04: Dijkstra implementation

        public override List<Link> FindShortestRouteBetween(string fromCity, string toCity, TransportModes mode)
        {
            return FindShortestRouteBetween(fromCity, toCity, mode, null);
        }

        public List<Link> FindShortestRouteBetween(string fromCity, string toCity, TransportModes mode,  IProgress<string> reportProgress)
        {
            var from = cities.FindCity(fromCity);
            var to = cities.FindCity(toCity);
            if(reportProgress != null)
                reportProgress.Report("Find cities by name - done");

            if (RouteRequestEvent != null)
            {
                if (from != null && to != null)
                    RouteRequestEvent(this, new RouteRequestEventArgs(from, to, mode));
            }

            var citiesBetween = cities.FindCitiesBetween(from, to);
            if (reportProgress != null)
                reportProgress.Report("Find cities between"+ from.Name + " and " + to.Name + " - done");
            if (citiesBetween == null || citiesBetween.Count < 1 || routes == null || routes.Count < 1)
                return null;

            var source = citiesBetween[0];
            var target = citiesBetween[citiesBetween.Count - 1];

            Dictionary<City, double> dist;
            Dictionary<City, City> previous;
            var q = FillListOfNodes(citiesBetween, out dist, out previous);
            dist[source] = 0.0;

            // the actual algorithm
            previous = SearchShortestPath(mode, q, dist, previous);
            if (reportProgress != null)
                reportProgress.Report("Route calculation - done");

            // create a list with all cities on the route
            var citiesOnRoute = GetCitiesOnRoute(source, target, previous);
            if (reportProgress != null)
                reportProgress.Report("Gather all cities on route - done");

            // prepare final list if links
            var path = FindPath(citiesOnRoute, mode);
            if (reportProgress != null)
                reportProgress.Report("Find path - done");

            return path;
        }

        private List<Link> FindPath(List<City> citiesOnRoute, TransportModes mode)
        {
            var linkedRoute = new List<Link>();
            for (int i = 0; i < citiesOnRoute.Count - 1; i++)
            {
                var distance = citiesOnRoute[i].Location.Distance(citiesOnRoute[i + 1].Location);
                linkedRoute.Add(new Link(citiesOnRoute[i], citiesOnRoute[i + 1], distance, mode));
            }
            return linkedRoute;
        }



        private static List<City> FillListOfNodes(List<City> cities, out Dictionary<City, double> dist, out Dictionary<City, City> previous)
        {
            var q = new List<City>(); // the set of all nodes (cities) in Graph ;
            dist = new Dictionary<City, double>();
            previous = new Dictionary<City, City>();
            foreach (var v in cities)
            {
                dist[v] = double.MaxValue;
                previous[v] = null;
                q.Add(v);
            }

            return q;
        }

        /// <summary>
        /// Searches the shortest path for cities and the given links
        /// </summary>
        /// <param name="mode">transportation mode</param>
        /// <param name="q"></param>
        /// <param name="dist"></param>
        /// <param name="previous"></param>
        /// <returns></returns>
        private Dictionary<City, City> SearchShortestPath(TransportModes mode, List<City> q, Dictionary<City, double> dist, Dictionary<City, City> previous)
        {
            while (q.Count > 0)
            {
                City u = null;
                var minDist = double.MaxValue;
                // find city u with smallest dist
                foreach (var c in q)
                    if (dist[c] < minDist)
                    {
                        u = c;
                        minDist = dist[c];
                    }

                if (u != null)
                {
                    q.Remove(u);
                    foreach (var n in FindNeighbours(u, mode))
                    {
                        var l = FindLink(u, n, mode);
                        var d = dist[u];
                        if (l != null)
                            d += l.Distance;
                        else
                            d += double.MaxValue;

                        if (dist.ContainsKey(n) && d < dist[n])
                        {
                            dist[n] = d;
                            previous[n] = u;
                        }
                    }
                }
                else
                    break;

            }

            return previous;
        }

        private Link FindLink(City u, City n, TransportModes mode)
        {
            var linkToFind = new Link(u, n, u.Location.Distance(n.Location), mode);
            var linkToFindRevert = new Link(n, u, n.Location.Distance(u.Location), mode);
            Predicate<Link> predicate = delegate(Link link)
            {
                return link.Equals(linkToFind) || link.Equals(linkToFindRevert);
            };
            return routes.Find(predicate);
        }


        /// <summary>
        /// Finds all neighbor cities of a city. 
        /// </summary>
        /// <param name="city">source city</param>
        /// <param name="mode">transportation mode</param>
        /// <returns>list of neighbor cities</returns>
        private List<City> FindNeighbours(City city, TransportModes mode)
        {
            var neighbors = new List<City>();
            foreach (var r in routes)
                if (mode.Equals(r.TransportMode))
                {
                    if (city.Equals(r.FromCity))
                        neighbors.Add(r.ToCity);
                    else if (city.Equals(r.ToCity))
                        neighbors.Add(r.FromCity);
                }

            return neighbors;
        }

        private List<City> GetCitiesOnRoute(City source, City target, Dictionary<City, City> previous)
        {
            var citiesOnRoute = new List<City>();
            var cr = target;
            while (previous[cr] != null)
            {
                citiesOnRoute.Add(cr);
                cr = previous[cr];
            }
            citiesOnRoute.Add(source);

            citiesOnRoute.Reverse();
            return citiesOnRoute;
        }
        #endregion
    }
}
