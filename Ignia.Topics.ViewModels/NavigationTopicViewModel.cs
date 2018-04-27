﻿/*==============================================================================================================================
| Author        Ignia, LLC
| Client        Ignia
| Project       Website
\=============================================================================================================================*/

using System.Collections.ObjectModel;

namespace Ignia.Topics.ViewModels {

  /*============================================================================================================================
  | VIEW MODEL: NAVIGATION TOPIC
  \---------------------------------------------------------------------------------------------------------------------------*/
  /// <summary>
  ///   Provides a strongly-typed data transfer object for feeding views with information about the navigation.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     No topics are expected to have a <c>Navigation</c> content type. Instead, this view model is expected to be manually
  ///     constructed by e.g. a <c>LayoutController</c>.
  ///   </para>
  ///   <para>
  ///     Since C# doesn't support return-type covariance, this class can't be derived in a meaningful way (i.e., if it were to
  ///     be, the <see cref="NavigationTopicViewModel.Children"/> property would still return a <see cref="Collection{T}"/> of
  ///     <see cref="NavigationTopicViewModel"/> instances). Instead, the preferred way to extend the functionality is to create
  ///     a new implementation of <see cref="INavigationTopicViewModelCore{T}"/>. To help communicate this, the <see
  ///     cref="NavigationTopicViewModel"/> class is marked as <c>sealed</c>.
  ///   </para>
  /// </remarks>
  public sealed class NavigationTopicViewModel : PageTopicViewModel, INavigationTopicViewModelCore<NavigationTopicViewModel> {

    public Collection<NavigationTopicViewModel> Children { get; set; }
    public bool IsSelected(string uniqueKey) => uniqueKey?.StartsWith(UniqueKey) ?? false;

  } // Class

} // Namespace