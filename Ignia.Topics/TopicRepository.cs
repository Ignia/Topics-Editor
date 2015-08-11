/*==============================================================================================================================
| Author        Casey Margell, Ignia LLC
| Client        Ignia, LLC
| Project       Topics Library
\=============================================================================================================================*/
using System;
using Ignia.Topics.Configuration;
using Ignia.Topics.Providers;

namespace Ignia.Topics {

  /*============================================================================================================================
  | CLASS: TOPIC REPOSITORY
  \---------------------------------------------------------------------------------------------------------------------------*/
  /// <summary>
  ///   The Topic Repository object provides access to a cached collection of Topic trees to support systems where we may want to
  ///   implement multiple taxonomies for different purposes.
  /// </summary>
  public static class TopicRepository {

  /*============================================================================================================================
  | PRIVATE VARIABLES
  \---------------------------------------------------------------------------------------------------------------------------*/
    private static      TopicDataProviderBase           _dataProvider           = null;
    private static      TopicMappingProviderBase        _mappingProvider        = null;
    private static      Topic                           _rootTopic              = null;
    private static      ContentTypeCollection           _contentTypes           = null;

    /*==========================================================================================================================
    | CONTENT TYPES
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Gets a list of available Content Types from the Configuration.
    /// </summary>
    /// <remarks>
    ///   ###TODO JJC092813: Need to identify a way of handling cache dependencies and/or recycling of ContentTypes based on
    ///   changes.
    /// </remarks>
    public static ContentTypeCollection ContentTypes {
      get {
        if (_contentTypes == null) {
          _contentTypes                 = new ContentTypeCollection();

          /*--------------------------------------------------------------------------------------------------------------------
          | Ensure root topic is available for use with supporting methods
          \-------------------------------------------------------------------------------------------------------------------*/
          if (RootTopic == null) throw new Exception("Root topic is null");

          /*--------------------------------------------------------------------------------------------------------------------
          | Ensure the parent ContentTypes topic is available to iterate over
          \-------------------------------------------------------------------------------------------------------------------*/
          if (RootTopic.GetTopic("Configuration:ContentTypes") == null) throw new Exception ("Configuration:ContentTypes");

          /*--------------------------------------------------------------------------------------------------------------------
          | Add available Content Types to the collection
          \-------------------------------------------------------------------------------------------------------------------*/
          foreach (Topic topic in RootTopic.GetTopic("Configuration:ContentTypes").FindAllByAttribute("ContentType", "ContentType", true)) {
          //Ensure the Topic is used as the strongly-typed ContentType
            ContentType contentType     = topic as ContentType;
          //Add ContentType Topic to collection if not already added
            if (contentType != null && !_contentTypes.Contains(contentType.Key)) {
              _contentTypes.Add(contentType);
            }
          }

        }
        return _contentTypes;
      }
      set {
        _contentTypes = value;
      }
    }

    /*==========================================================================================================================
    | ROOT TOPIC
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Static reference to the root <see cref="Topic"/>.
    /// </summary>
    public static Topic RootTopic {
      get {
        if (_rootTopic == null) {
          _rootTopic = Topic.Load();
        }
        return _rootTopic;
      }
      set {
        _rootTopic = value;
      }
    }

    /*==========================================================================================================================
    | TOPIC PROVIDER
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Getter for the data provider that we're using.
    /// </summary>
    /// <remarks>
    ///   Pulled from the <see cref="Ignia.Topics.Configuration.TopicDataProviderManager"/>.
    /// </remarks>
    public static TopicDataProviderBase DataProvider {
      get {
        if (_dataProvider == null) {
          _dataProvider = TopicDataProviderManager.DataProvider;
          MappingProvider.DataProvider = _dataProvider;
        }
        return _dataProvider;
      }
      set {
        _dataProvider = value;
      }
    }

    /*==========================================================================================================================
    | MAPPING PROVIDER
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Getter for the data mapping provider that we're using.
    /// </summary>
    /// <remarks>
    ///   Pulled from the <see cref="Ignia.Topics.Configuration.TopicMappingProviderManager"/>.
    /// </remarks>
    public static TopicMappingProviderBase MappingProvider {
      get {
        if (_mappingProvider == null) {
          _mappingProvider = TopicMappingProviderManager.MappingProvider;
        }
        return _mappingProvider;
      }
    }

    /*==========================================================================================================================
    | METHOD: LOAD
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Static method that passes Load requests along to the current topic data provider.
    /// </summary>
    /// <remarks>
    ///   Optional overload allows for the topic's ID to be specified rather than the string uniqueKey value of the topic.
    /// </remarks>
    /// <param name="topic">String uniqueKey identifier for the topic.</param>
    /// <param name="topicId">Integer identifier for the topic.</param>
    /// <param name="depth">Integer level to which to also load the topic's children.</param>
    /// <param name="version">DateTime identifier for the version of the topic.</param>
    public static Topic Load(string topic, int depth = 0, DateTime? version = null) {
      return DataProvider.Load(topic, depth, version);
    }

    public static Topic Load(int topicId, int depth = 0, DateTime? version = null) {
      return DataProvider.Load(topicId, depth, version);
    }

    /*==========================================================================================================================
    | METHOD: SAVE
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Static method that passes Save requests along to the current topic data provider.
    /// </summary>
    /// <param name="topic">The topic object.</param>
    /// <param name="isRecursive">
    ///   Boolean indicator nothing whether to recurse through the topic's children and save them as well.
    /// </param>
    /// <param name="isDraft">Boolean indicator as to the topic's publishing status.</param>
    public static int Save(Topic topic, bool isRecursive, bool isDraft = false) {
      return DataProvider.Save(topic, isRecursive, isDraft);
    }

    /*==========================================================================================================================
    | METHOD: MOVE
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Static method that passes Move requests along to the current topic data provider.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     For ordering: Optionally accepts the sibling topic to place topic behind. Defaults to placing topic in front of
    ///     existing siblings.
    ///   </para>
    ///   <para>
    ///     Optional overload allows for specifying a sibling topic.
    ///   </para>
    /// </remarks>
    /// <param name="topic">The topic object to be moved.</param>
    /// <param name="target">A topic object under which to move the source topic.</param>
    /// <param name="sibling">A topic object representing a sibling adjacent to which the topic should be moved.</param>
    public static bool Move(Topic topic, Topic target) {
      ReorderSiblings(topic);
      return DataProvider.Move(topic, target);
    }

    public static bool Move(Topic topic, Topic target, Topic sibling) {
      ReorderSiblings(topic, sibling);
      return DataProvider.Move(topic, target, sibling);
    }

    /*==========================================================================================================================
    | METHOD: REORDER SIBLINGS
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Static method that updates the sort order of topics at a particular level.
    /// </summary>
    /// <param name="source">The topic object representing the reordering point.</param>
    /// <param name="sibling">
    ///   The topic object that if provided, represents the topic after which the source topic should be ordered.
    /// </param>
    private static void ReorderSiblings(Topic source) {
      ReorderSiblings(source, null);
    }

    private static void ReorderSiblings(Topic source, Topic sibling) {

      Topic   parent          = source.Parent;
      int     sortOrder       = -1;

      /*------------------------------------------------------------------------------------------------------------------------
      | If there is no sibling, inject the source at the beginning of the collection
      \-----------------------------------------------------------------------------------------------------------------------*/
      if (sibling == null) {
        source.SortOrder = sortOrder++;
      }

      /*------------------------------------------------------------------------------------------------------------------------
      | Loop through each topic to assign a new priority order
      \-----------------------------------------------------------------------------------------------------------------------*/
      foreach (Topic topic in parent.SortedChildren) {
      //Assuming the topic isn't the source, increment the sortOrder
        if (topic != source) {
          topic.SortOrder = sortOrder++;
        }
      //If the topic is the sibling, then assign the next sortOrder to the source
        if (topic == sibling) {
          source.SortOrder = sortOrder++;
        }
      }

    }

    /*==========================================================================================================================
    | METHOD: DELETE
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Static method that passes Delete requests along to the current topic data provider.
    /// </summary>
    /// <param name="topic">The topic object to delete.</param>
    /// <param name="isRecursive">
    ///   Boolean indicator nothing whether to recurse through the topic's children and delete them as well.
    /// </param>
    public static void Delete(Topic topic) {
      Delete(topic, true);
    }

    public static void Delete(Topic topic, bool isRecursive) {
      DataProvider.Delete(topic, isRecursive);
    }

  } // Class

} //Namespace
