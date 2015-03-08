﻿using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;
using Skobbler.Ngx;
using Skobbler.Ngx.Packages;
using Skobbler.Ngx.Search;
using System.Collections.Generic;
using System.Linq;

namespace Skobbler.SDKDemo.Activities
{
    /// <summary>
    /// Activity where offline address searches are performed and results are listed
    /// </summary>
    [Activity(ConfigurationChanges = ConfigChanges.Orientation)]
    public class OfflineAddressSearchActivity : Activity, ISKSearchListener
    {

        /// <summary>
        /// The current list level (see)
        /// </summary>
        private short currentListLevel;

        /// <summary>
        /// Top level packages available offline (countries and US states)
        /// </summary>
        private IList<SKPackage> packages;

        private ListView listView;

        private TextView operationInProgressLabel;

        private ResultsListAdapter adapter;

        /// <summary>
        /// Offline address search results grouped by level
        /// </summary>
        private IDictionary<short?, IList<SKSearchResult>> resultsPerLevel = new Dictionary<short?, IList<SKSearchResult>>();

        /// <summary>
        /// Current top level package code
        /// </summary>
        private string currentCountryCode;

        /// <summary>
        /// Search manager object
        /// </summary>
        private SKSearchManager searchManager;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_list);

            operationInProgressLabel = (TextView)FindViewById(Resource.Id.label_operation_in_progress);
            listView = (ListView)FindViewById(Resource.Id.list_view);
            operationInProgressLabel.Text = Resources.GetString(Resource.String.searching);

            packages = SKPackageManager.Instance.GetInstalledPackages().ToList();
            searchManager = new SKSearchManager(this);

            if (packages.Count == 0)
            {
                Toast.MakeText(this, "No offline map packages are available", ToastLength.Short).Show();
            }

            initializeList();
        }

        /// <summary>
        /// Initializes list with top level packages
        /// </summary>
        private void initializeList()
        {
            adapter = new ResultsListAdapter(this);
            listView.Adapter = adapter;
            operationInProgressLabel.Visibility = ViewStates.Gone;
            listView.Visibility = ViewStates.Visible;

            listView.ItemClick += (s, e) =>
            {
                if (currentListLevel == 0)
                {
                    currentCountryCode = packages[e.Position].Name;
                    changeLevel((short)(currentListLevel + 1), -1, currentCountryCode);
                }
                else if (currentListLevel < 3)
                {
                    changeLevel((short)(currentListLevel + 1), resultsPerLevel[currentListLevel][e.Position].Id, currentCountryCode);
                }
            };
        }

        /// <summary>
        /// Changes the list level and executes the corresponding action for the list
        /// level change
        /// </summary>
        /// <param name="newLevel">    the new level </param>
        /// <param name="parentId">    the parent id for which to execute offline address search </param>
        /// <param name="countryCode"> the current code to use in offline address search </param>
        private void changeLevel(short newLevel, long parentId, string countryCode)
        {
            if (newLevel == 0 || newLevel < currentListLevel)
            {
                // for new list level 0 or smaller than previous one just change the
                // level and update the adapter
                operationInProgressLabel.Visibility = ViewStates.Gone;
                listView.Visibility = ViewStates.Visible;
                currentListLevel = newLevel;
                adapter.NotifyDataSetChanged();
            }
            else if (newLevel > currentListLevel && newLevel > 0)
            {
                // for new list level greater than previous one execute an offline
                // address search
                operationInProgressLabel.Visibility = ViewStates.Visible;
                listView.Visibility = ViewStates.Gone;
                // get a search object
                SKMultiStepSearchSettings searchObject = new SKMultiStepSearchSettings();
                // set the maximum number of results to be returned
                searchObject.MaxSearchResultsNumber = 25;
                // set the country code
                searchObject.OfflinePackageCode = currentCountryCode;
                // set the search term
                searchObject.SearchTerm = "";
                // set the id of the parent node in which to search
                searchObject.ParentIndex = parentId;
                // set the list level
                searchObject.ListLevel = SKSearchManager.SKListLevel.ForInt(newLevel + 1);
                // change the list level to the new one
                currentListLevel = newLevel;
                // initiate the search
                searchManager.MultistepSearch(searchObject);
            }
        }

        public void OnReceivedSearchResults(IList<SKSearchResult> results)
        {
            // put in the map at the corresponding level the received results
            resultsPerLevel[currentListLevel] = results;
            operationInProgressLabel.Visibility = ViewStates.Gone;
            listView.Visibility = ViewStates.Visible;
            if (results.Count > 0)
            {
                // received results - update adapter to show the results
                adapter.NotifyDataSetChanged();
            }
            else
            {
                // zero results - no change
                currentListLevel--;
                adapter.NotifyDataSetChanged();
            }
        }

        public override void OnBackPressed()
        {
            if (currentListLevel == 0)
            {
                base.OnBackPressed();
            }
            else
            {
                // if not top level - decrement the current list level and show
                // results for the new level
                changeLevel((short)(currentListLevel - 1), -1, currentCountryCode);
            }
        }

        private class ResultsListAdapter : BaseAdapter<SKPackage>
        {
            private readonly OfflineAddressSearchActivity outerInstance;

            public ResultsListAdapter(OfflineAddressSearchActivity outerInstance)
            {
                this.outerInstance = outerInstance;
            }


            public override int Count
            {
                get
                {
                    if (outerInstance.currentListLevel > 0)
                    {
                        return outerInstance.resultsPerLevel[outerInstance.currentListLevel].Count;
                    }
                    else
                    {
                        return outerInstance.packages.Count;
                    }
                }
            }

            public override long GetItemId(int position)
            {
                return 0;
            }

            public override View GetView(int position, View convertView, ViewGroup parent)
            {
                View view = null;
                if (convertView == null)
                {
                    LayoutInflater inflater = (LayoutInflater)outerInstance.GetSystemService(Context.LayoutInflaterService);
                    view = inflater.Inflate(Resource.Layout.layout_search_list_item, null);
                }
                else
                {
                    view = convertView;
                }
                if (outerInstance.currentListLevel > 0)
                {
                    // for offline address search results show the result name and
                    // position
                    ((TextView)view.FindViewById(Resource.Id.title)).Text = outerInstance.resultsPerLevel[outerInstance.currentListLevel][position].Name;
                    SKCoordinate location = outerInstance.resultsPerLevel[outerInstance.currentListLevel][position].Location;
                    ((TextView)view.FindViewById(Resource.Id.subtitle)).Visibility = ViewStates.Visible;
                    ((TextView)view.FindViewById(Resource.Id.subtitle)).Text = "location: (" + location.Latitude + ", " + location.Longitude + ")";
                }
                else
                {
                    ((TextView)view.FindViewById(Resource.Id.title)).Text = outerInstance.packages[position].Name;
                    ((TextView)view.FindViewById(Resource.Id.subtitle)).Visibility = ViewStates.Gone;
                }
                return view;
            }

            public override SKPackage this[int position]
            {
                get
                {
                    if (outerInstance.currentListLevel > 0)
                    {
                        //TODO;
                        return outerInstance.packages[position];
                    }
                    else
                    {
                        return outerInstance.packages[position];
                    }
                }
            }
        }
    }

}