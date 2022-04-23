﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Uno.Extensions.Collections;
using Uno.Extensions.Reactive.Bindings.Collections._BindableCollection;
using Uno.Extensions.Reactive.Bindings.Collections._BindableCollection.Data;
using Uno.Extensions.Reactive.Bindings.Collections._BindableCollection.Facets;
using Uno.Extensions.Reactive.Dispatching;
using Uno.Extensions.Reactive.Utils;

#if WINUI
using CurrentChangingEventHandler = Microsoft.UI.Xaml.Data.CurrentChangingEventHandler;
using CurrentChangedEventHandler = System.EventHandler<object>;
using ISchedulersProvider = System.Func<Microsoft.UI.Dispatching.DispatcherQueue?>;
#elif HAS_WINDOWS_UI || HAS_UMBRELLA_UI || true
using CurrentChangingEventHandler = Windows.UI.Xaml.Data.CurrentChangingEventHandler;
using CurrentChangedEventHandler = System.EventHandler<object>;
using ISchedulersProvider = System.Func<Windows.System.DispatcherQueue?>;
#else
using CurrentChangingEventHandler = System.ComponentModel.CurrentChangingEventHandler;
using CurrentChangingEventArgs = System.ComponentModel.CurrentChangingEventArgs;
using CurrentChangedEventHandler = System.EventHandler;
#endif

namespace Uno.Extensions.Reactive.Bindings.Collections
{
	/// <summary>
	/// A collection which is responsible to manage the items tracking.
	/// </summary>
	internal sealed partial class BindableCollection : ICollectionView, INotifyCollectionChanged
	{
		private readonly IBindableCollectionDataStructure _dataStructure;
		private readonly DispatcherLocal<DataLayer> _holder;

		private IObservableCollection? _current;

		/// <summary>
		/// Creates a new instance of a <see cref="BindableCollection"/>.
		/// </summary>
		/// <param name="initial">The initial items in the collection.</param>
		/// <param name="itemComparer">
		/// Comparer used to detect multiple versions of the **same entity (T)**, or null to use default.
		/// <remarks>Usually this should only compare the ID of the entities in order to properly track the changes made on an entity.</remarks>
		/// <remarks>For better performance, prefer provide null instead of <see cref="EqualityComparer{T}.Default"/> (cf. MapObservableCollection).</remarks>
		/// </param>
		/// <param name="itemVersionComparer">
		/// Comparer used to detect multiple instance of the **same version** of the **same entity (T)**, or null to rely only on the <paramref name="itemComparer"/> (not recommanded).
		/// <remarks>
		/// This comparer will determine if two instances of the same entity (which was considered as equals by the <paramref name="itemComparer"/>),
		/// are effectively equals or not (i.e. same version or not).
		/// <br />
		/// * If **Equals**: it's 2 **instances** of the **same version** of the **same entity** (all properties are equals), so we don't have to raise a <see cref="NotifyCollectionChangedAction.Replace"/>.<br />
		/// * If **NOT Equals**: it's 2 **distinct versions** of the **same entity** (not all properties are equals) and we have to raise a 'Replace' to re-evaluate those properties.
		/// </remarks>
		/// </param>
		/// <param name="schedulersProvider">Schedulers provider to use to handle concurrency.</param>
		/// <param name="resetThreshold">Threshold on which the a single reset is raised instead of multiple collection changes.</param>
		internal static BindableCollection Create<T>(
			IObservableCollection<T>? initial = null,
			IEqualityComparer<T>? itemComparer = null,
			IEqualityComparer<T>? itemVersionComparer = null,
			ISchedulersProvider? schedulersProvider = null,
			int resetThreshold = DataStructure.DefaultResetThreshold)
		{
			var dataStructure = new DataStructure
			(
				(itemComparer?.ToEqualityComparer(), itemVersionComparer?.ToEqualityComparer())
			)
			{
				ResetThreshold = resetThreshold
			};

			return new BindableCollection(dataStructure, initial, schedulersProvider);
		}

		/// <summary>
		/// Creates a new instance of a <see cref="BindableCollection"/>, without needing to specify the element type.
		/// </summary>
		internal static BindableCollection CreateUntyped(
			IObservableCollection? initial = null,
			IEqualityComparer? itemComparer = null,
			IEqualityComparer? itemVersionComparer = null,
			ISchedulersProvider? schedulersProvider = null,
			int resetThreshold = DataStructure.DefaultResetThreshold
		)
		{
			var dataStructure = new DataStructure
			(
				(itemComparer, itemVersionComparer)
			)
			{
				ResetThreshold = resetThreshold
			};

			return new BindableCollection(dataStructure, initial, schedulersProvider);
		}

		internal static BindableCollection CreateGrouped<TKey, T>(
			IObservableCollection<TKey> initial,
			IEqualityComparer<TKey>? keyComparer,
			IEqualityComparer<TKey>? keyVersionComparer,
			IEqualityComparer<T>? itemComparer = null,
			IEqualityComparer<T>? itemVersionComparer = null,
			ISchedulersProvider? schedulersProvider = null,
			int resetThreshold = DataStructure.DefaultResetThreshold)
			where TKey : IObservableGroup<T>
		{
			var dataStructure = new DataStructure
			(
				(keyComparer?.ToEqualityComparer(), keyVersionComparer?.ToEqualityComparer()),
				(itemComparer?.ToEqualityComparer(), itemVersionComparer?.ToEqualityComparer())
			)
			{
				ResetThreshold = resetThreshold
			};

			return new BindableCollection(dataStructure, initial, schedulersProvider);
		}

		internal static BindableCollection CreateGrouped<TGroup>(
			IObservableCollection initial,
			IEqualityComparer<TGroup>? groupComparer,
			IEqualityComparer<TGroup>? groupVersionComparer,
			IEqualityComparer? itemComparer,
			IEqualityComparer? itemVersionComparer,
			ISchedulersProvider? schedulersProvider = null,
			int resetThreshold = DataStructure.DefaultResetThreshold)
			where TGroup : IObservableGroup
		{
			var dataStructure = new DataStructure
			(
				(groupComparer?.ToEqualityComparer(), groupVersionComparer?.ToEqualityComparer()),
				(itemComparer, itemVersionComparer)
			)
			{
				ResetThreshold = resetThreshold
			};

			return new BindableCollection(dataStructure, initial, schedulersProvider);
		}

		internal static BindableCollection CreateUntypedGrouped(
			IObservableCollection initial,
			IEqualityComparer? groupComparer,
			IEqualityComparer? groupVersionComparer,
			IEqualityComparer? itemComparer,
			IEqualityComparer? itemVersionComparer,
			ISchedulersProvider? schedulersProvider = null,
			int resetThreshold = DataStructure.DefaultResetThreshold)
		{
			var dataStructure = new DataStructure
			(
				(groupComparer, groupVersionComparer),
				(itemComparer, itemVersionComparer)
			)
			{
				ResetThreshold = resetThreshold
			};

			return new BindableCollection(dataStructure, initial, schedulersProvider);
		}

		internal BindableCollection(
			IBindableCollectionDataStructure dataStructure,
			IObservableCollection? initial = null,
			ISchedulersProvider? schedulersProvider = null)
		{
			_dataStructure = dataStructure;
			_current = initial;
			_holder = new DispatcherLocal<DataLayer>(context => DataLayer.Create(_dataStructure.GetRoot(), _current ?? EmptyObservableCollection<object>.Instance, context), schedulersProvider);
		}

		/// <summary>
		/// Reset the collection of items of the collection
		/// </summary>
		/// <param name="source">The new source to use</param>
		/// <param name="mode"></param>
		internal void Switch(IObservableCollection? source, TrackingMode mode = TrackingMode.Auto)
		{
			source ??= EmptyObservableCollection<object>.Instance;

			// Set it as current immediately so if the collection is accessed from another thread, it will start with the right version
			var original = Interlocked.Exchange(ref _current, source);
			if (object.ReferenceEquals(source, original))
			{
				return;
			}

			lock (_holder)
			{
				foreach (var holder in _holder.GetValues())
				{
					holder.value.Update(source, mode);
				}
			}
		}

		/// <summary>
		/// Get a direct access to the ICollectionView implementation for a given thread.
		/// </summary>
		/// <returns></returns>
		public ICollectionView GetForCurrentThread()
			=> _holder.Value.View;

		#region ICollectionView
		/// <inheritdoc />
		public IEnumerator<object> GetEnumerator()
			=> GetForCurrentThread().GetEnumerator();

		/// <inheritdoc />
		IEnumerator IEnumerable.GetEnumerator()
			=> ((IEnumerable) GetForCurrentThread()).GetEnumerator();

		/// <inheritdoc />
		public void Add(object? item)
			=> GetForCurrentThread().Add(item);

		/// <inheritdoc />
		public void Clear()
			=> GetForCurrentThread().Clear();

		/// <inheritdoc />
		public bool Contains(object? item)
			=> GetForCurrentThread().Contains(item);

		/// <inheritdoc />
		public void CopyTo(object?[] array, int arrayIndex)
			=> GetForCurrentThread().CopyTo(array, arrayIndex);

		/// <inheritdoc />
		public bool Remove(object? item)
			=> GetForCurrentThread().Remove(item);

		/// <inheritdoc />
		public int Count => GetForCurrentThread().Count;

		/// <inheritdoc />
		public bool IsReadOnly => GetForCurrentThread().IsReadOnly;

		/// <inheritdoc />
		public int IndexOf(object? item)
			=> GetForCurrentThread().IndexOf(item);

		/// <inheritdoc />
		public void Insert(int index, object? item)
			=> GetForCurrentThread().Insert(index, item);

		/// <inheritdoc />
		public void RemoveAt(int index)
			=> GetForCurrentThread().RemoveAt(index);

		/// <inheritdoc />
		public object? this[int index]
		{
#pragma warning disable CS8766 // ICollectionView should be IList<object?>
			get { return GetForCurrentThread()[index]; }
#pragma warning restore CS8766
			set { GetForCurrentThread()[index] = value; }
		}

		/// <inheritdoc />
		public event VectorChangedEventHandler<object?>? VectorChanged
		{
			// We directly hit the facet to ease the return type mismatch
			add => _holder.Value.GetFacet<CollectionChangedFacet>().AddVectorChangedHandler(value!);
			remove => _holder.Value.GetFacet<CollectionChangedFacet>().RemoveVectorChangedHandler(value!);
		}

		/// <inheritdoc />
		public event NotifyCollectionChangedEventHandler? CollectionChanged
		{
			// We directly hit the facet to ease the return type mismatch
			add => _holder.Value.GetFacet<CollectionChangedFacet>().AddCollectionChangedHandler(value!);
			remove => _holder.Value.GetFacet<CollectionChangedFacet>().RemoveCollectionChangedHandler(value!);
		}

		/// <inheritdoc />
		public bool MoveCurrentTo(object item)
			=> GetForCurrentThread().MoveCurrentTo(item);

		/// <inheritdoc />
		public bool MoveCurrentToPosition(int index)
			=> GetForCurrentThread().MoveCurrentToPosition(index);

		/// <inheritdoc />
		public bool MoveCurrentToFirst()
			=> GetForCurrentThread().MoveCurrentToFirst();

		/// <inheritdoc />
		public bool MoveCurrentToLast()
			=> GetForCurrentThread().MoveCurrentToLast();

		/// <inheritdoc />
		public bool MoveCurrentToNext()
			=> GetForCurrentThread().MoveCurrentToNext();

		/// <inheritdoc />
		public bool MoveCurrentToPrevious()
			=> GetForCurrentThread().MoveCurrentToPrevious();

		/// <inheritdoc />
		public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
			=> GetForCurrentThread().LoadMoreItemsAsync(count);

		/// <inheritdoc />
		public IObservableVector<object> CollectionGroups => GetForCurrentThread().CollectionGroups;

		/// <inheritdoc />
		public object? CurrentItem => GetForCurrentThread().CurrentItem;

		/// <inheritdoc />
		public int CurrentPosition => GetForCurrentThread().CurrentPosition;

		/// <inheritdoc />
		public bool HasMoreItems => GetForCurrentThread().HasMoreItems;

		/// <inheritdoc />
		public bool IsCurrentAfterLast => GetForCurrentThread().IsCurrentAfterLast;

		/// <inheritdoc />
		public bool IsCurrentBeforeFirst => GetForCurrentThread().IsCurrentBeforeFirst;

		/// <inheritdoc />
		public event CurrentChangedEventHandler? CurrentChanged
		{
			// We directly hit the facet to ease the retrun type mismatch
			add => _holder.Value.GetFacet<SelectionFacet>().AddCurrentChangedHandler(value!);
			remove => _holder.Value.GetFacet<SelectionFacet>().RemoveCurrentChangedHandler(value!);
		}

		/// <inheritdoc />
		public event CurrentChangingEventHandler? CurrentChanging
		{
			// We directly hit the facet to ease the retrun type mismatch
			add => _holder.Value.GetFacet<SelectionFacet>().AddCurrentChangingHandler(value!);
			remove => _holder.Value.GetFacet<SelectionFacet>().RemoveCurrentChangingHandler(value!);
		}
		#endregion

		internal EventRegistrationToken AddVectorChangedHandler(VectorChangedEventHandler<object?>? handler)
			=> _holder.Value.GetFacet<CollectionChangedFacet>().AddVectorChangedHandler(handler!);
		internal void RemoveVectorChangedHandler(VectorChangedEventHandler<object?>? handler)
			=> _holder.Value.GetFacet<CollectionChangedFacet>().RemoveVectorChangedHandler(handler!);
		internal void RemoveVectorChangedHandler(EventRegistrationToken token)
			=> _holder.Value.GetFacet<CollectionChangedFacet>().RemoveVectorChangedHandler(token);

		internal EventRegistrationToken AddCurrentChangedHandler(CurrentChangedEventHandler? handler)
			=> _holder.Value.GetFacet<SelectionFacet>().AddCurrentChangedHandler(handler!);
		internal void RemoveCurrentChangedHandler(CurrentChangedEventHandler? handler)
			=> _holder.Value.GetFacet<SelectionFacet>().RemoveCurrentChangedHandler(handler!);
		internal void RemoveCurrentChangedHandler(EventRegistrationToken token)
			=> _holder.Value.GetFacet<SelectionFacet>().RemoveCurrentChangedHandler(token);

		internal EventRegistrationToken AddCurrentChangingHandler(CurrentChangingEventHandler? handler)
			=> _holder.Value.GetFacet<SelectionFacet>().AddCurrentChangingHandler(handler!);
		internal void RemoveCurrentChangingHandler(CurrentChangingEventHandler? handler)
			=> _holder.Value.GetFacet<SelectionFacet>().RemoveCurrentChangingHandler(handler!);
		internal void RemoveCurrentChangingHandler(EventRegistrationToken token)
			=> _holder.Value.GetFacet<SelectionFacet>().RemoveCurrentChangingHandler(token);


		/// <summary>
		/// Gets some extended properties for this collection
		/// </summary>
		/// <remarks>Conversely to the properties of this class, properties exposed by this bag are bindable</remarks>
		public BindableCollectionExtendedProperties ExtendedProperties => _holder.Value.GetFacet<BindableCollectionExtendedProperties>();
	}
}