﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using Xamarin.Forms;
using Xamarin.Forms.GoogleMaps;

namespace TripRecorder2.Views
{
    public class BindableMap : Map
    {
        public BindableMap()
        {
            this.CameraIdled += BindableMap_CameraIdled;
        }

        private bool IsUpdatingCenter = false;
        private void BindableMap_CameraIdled(object sender, CameraIdledEventArgs e)
        {
            if (IsUpdatingCenter)
                return;
            try
            {
                IsUpdatingCenter = true;
                Center = e.Position.Target;
            }
            finally
            {
                IsUpdatingCenter = false;
            }
        }

        public static readonly BindableProperty CenterProperty = BindableProperty.Create(
            nameof(Center), typeof(Position), typeof(BindableMap), new Position(),
            propertyChanged: (b, o, n) => ((BindableMap)b).OnCenterChanged((Position)n));
        public Position Center
        {
            get => (Position)GetValue(CenterProperty);
            set => SetValue(CenterProperty, value);
        }

        private void OnCenterChanged(Position newPosition)
        {
            if (IsUpdatingCenter)
                return;
            try
            {
                IsUpdatingCenter = true;
                MoveCamera(CameraUpdateFactory.NewPosition(newPosition));
            }
            finally
            {
                IsUpdatingCenter = false;
            }
        }

        public static readonly BindableProperty CirclesSourceProperty = BindableProperty.Create(
            nameof(CirclesSource), typeof(IEnumerable<Circle>), typeof(BindableMap), null,
            propertyChanged: (b, o, n) =>
            ((BindableMap)b).OnCirclesSourceChanged(
            (IEnumerable<Circle>)o, (IEnumerable<Circle>)n));
        public IEnumerable<Circle> CirclesSource
        {
            get => (IEnumerable<Circle>)GetValue(CirclesSourceProperty);
            set => SetValue(CirclesSourceProperty, value);
        }

        private void OnCirclesSourceChanged(
            IEnumerable<Circle> oldItemsSource, IEnumerable<Circle> newItemsSource)
        {
            if (oldItemsSource is INotifyCollectionChanged ncc)
            {
                ncc.CollectionChanged -= OnCirclesSourceCollectionChanged;
            }

            if (newItemsSource is INotifyCollectionChanged ncc1)
            {
                ncc1.CollectionChanged += OnCirclesSourceCollectionChanged;
            }

            
        }

        private void OnCirclesSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewStartingIndex == -1)
                        goto case NotifyCollectionChangedAction.Reset;
                    foreach (Circle item in e.NewItems)
                        Circles.Add(item);
                    break;
                case NotifyCollectionChangedAction.Move:
                    if (e.OldStartingIndex == -1 || e.NewStartingIndex == -1)
                        goto case NotifyCollectionChangedAction.Reset;
                    // Not tracking order
                    break;
                case NotifyCollectionChangedAction.Remove:
                    if (e.OldStartingIndex == -1)
                        goto case NotifyCollectionChangedAction.Reset;
                    foreach (Circle item in e.OldItems)
                        Circles.Remove(item);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    if (e.OldStartingIndex == -1)
                        goto case NotifyCollectionChangedAction.Reset;
                    foreach (Circle item in e.OldItems)
                        Circles.Remove(item);
                    foreach (Circle item in e.NewItems)
                        Circles.Add(item);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    Circles.Clear();
                    break;
            }
        }

        private void UpdateCircles()
        {
            Circles.Clear();
            if (CirclesSource?.Any() ?? false)
            {
                foreach (var circle in CirclesSource)
                {
                    Circles.Add(circle);
                }
            }
        }
    }
}
