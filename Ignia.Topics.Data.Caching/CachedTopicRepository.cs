﻿/*==============================================================================================================================
| Author        Ignia, LLC
| Client        Ignia, LLC
| Project       Topics Library
\=============================================================================================================================*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ignia.Topics.Repositories;
using System.Diagnostics.Contracts;
using System.Configuration;
using System.Data;
using System.Xml;
using System.Globalization;

namespace Ignia.Topics.Data.Caching {

  /*============================================================================================================================
  | CLASS: SQL TOPIC DATA REPOSITORY
  \---------------------------------------------------------------------------------------------------------------------------*/
  /// <summary>
  ///   Provides data access to topics stored in Microsoft SQL Server.
  /// </summary>
  /// <remarks>
  ///   Concrete implementation of the <see cref="Ignia.Topics.Repositories.IDataRepository"/> class.
  /// </remarks>

  public class CachedTopicRepository : TopicRepositoryBase, ITopicRepository {

    /*==========================================================================================================================
    | VARIABLES
    \-------------------------------------------------------------------------------------------------------------------------*/
    ITopicRepository            _dataProvider            = null;
    Topic                       _cache                   = null;

    /*==========================================================================================================================
    | CONSTRUCTOR
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Instantiates a new instance of the CachedTopicRepository with a dependency on an underlying ITopicRepository in order 
    ///   to provide necessary data access. 
    /// </summary>
    /// <param name="dataProvider">A concrete instance of an ITopicRepository, which will be used for data access.</param>
    /// <returns>A new instance of the CachedTopicRepository.</returns>
    public CachedTopicRepository(ITopicRepository dataProvider) : base() {
      _dataProvider = dataProvider;
    }

    /*==========================================================================================================================
    | GET CONTENT TYPES
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Retrieves a collection of Content Type objects from the configuration section of the data provider.
    /// </summary>
    public override ContentTypeCollection GetContentTypes() {
      return _dataProvider.GetContentTypes();
    }

    /*==========================================================================================================================
    | METHOD: LOAD
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Interface method that loads topics into memory.
    /// </summary>
    /// <param name="topicKey">The string identifier for the topic.</param>
    /// <param name="topicId">The integer identifier for the topic.</param>
    /// <param name="depth">The level to which to recurse through and load a topic's children.</param>
    /// <param name="version">The DateTime stamp signifying when the topic was saved.</param>
    /// <returns>A topic object.</returns>
    /// <exception cref="Exception">
    ///   The topic Ignia.Topics.<c>contentType</c> does not derive from Ignia.Topics.Topic.
    /// </exception>
    /// <exception cref="Exception">
    ///   Topics failed to load: <c>ex.Message</c>
    /// </exception>
    protected override Topic Load(string topicKey, int topicId, int depth, DateTime? version = null) {

      /*------------------------------------------------------------------------------------------------------------------------
      | Validate contracts
      \-----------------------------------------------------------------------------------------------------------------------*/
      Contract.Ensures(Contract.Result<Topic>() != null);

      /*------------------------------------------------------------------------------------------------------------------------
      | Relay version requests to data provider
      \-----------------------------------------------------------------------------------------------------------------------*/
      if (version != null) {
        if (topicKey == null) {
          return _dataProvider.Load(topicId, depth, version);
        }
        else {
          return _dataProvider.Load(topicKey, depth, version);
        }
      }

      /*------------------------------------------------------------------------------------------------------------------------
      | Ensure topics are loaded
      \-----------------------------------------------------------------------------------------------------------------------*/
      if (_cache == null) {
        _cache = _dataProvider.Load();
      }

      /*------------------------------------------------------------------------------------------------------------------------
      | Lookup by TopicId
      \-----------------------------------------------------------------------------------------------------------------------*/
      if (topicId >= 0) {
        return _cache.GetTopic(topicId);
      }

      /*------------------------------------------------------------------------------------------------------------------------
      | Lookup by TopicKey
      \-----------------------------------------------------------------------------------------------------------------------*/
      if (!String.IsNullOrWhiteSpace(topicKey)) {
        return _cache.GetTopic(topicKey);
      }

      /*------------------------------------------------------------------------------------------------------------------------
      | Return entire cache
      \-----------------------------------------------------------------------------------------------------------------------*/
      return _cache;

    }

    /*==========================================================================================================================
    | METHOD: SAVE
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Interface method that saves topic attributes; also used for renaming a topic since name is stored as an attribute.
    /// </summary>
    /// <param name="topic">The topic object.</param>
    /// <param name="isRecursive">
    ///   Boolean indicator nothing whether to recurse through the topic's descendants and save them as well.
    /// </param>
    /// <param name="isDraft">Boolean indicator as to the topic's publishing status.</param>
    /// <returns>The integer return value from the execution of the <c>topics_UpdateTopic</c> stored procedure.</returns>
    /// <exception cref="Exception">
    ///   The Content Type <c>topic.Attributes.Get(ContentType, Page)</c> referenced by <c>topic.Key</c> could not be found under 
    ///   Configuration:ContentTypes. There are <c>TopicRepository.ContentTypes.Count</c> ContentTypes in the Repository.
    /// </exception>
    /// <exception cref="Exception">
    ///   Failed to save Topic <c>topic.Key</c> (<c>topic.Id</c>) via 
    ///   <c>ConfigurationManager.ConnectionStrings[TopicsServer].ConnectionString</c>: <c>ex.Message</c>
    /// </exception>
    public override int Save(Topic topic, bool isRecursive = false, bool isDraft = false) {
      return _dataProvider.Save(topic, isRecursive, isDraft);
    }

    /*==========================================================================================================================
    | METHOD: MOVE
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Interface method that supports moving a topic from one position to another.
    /// </summary>
    /// <remarks>
    ///   May optionally specify a sibling. If specified, it is expected that the topic will be placed immediately after the 
    ///   topic. 
    /// </remarks>
    /// <param name="topic">The topic object to be moved.</param>
    /// <param name="target">The target (parent) topic object under which the topic should be moved.</param>
    /// <param name="sibling">A topic object representing a sibling adjacent to which the topic should be moved.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
      "Microsoft.Contracts",
      "TestAlwaysEvaluatingToAConstant",
      Justification = "Sibling may be null from overloaded caller."
      )]
    public override void Move(Topic topic, Topic target, Topic sibling) {
      _dataProvider.Move(topic, target, sibling);
    }

    /*==========================================================================================================================
    | METHOD: DELETE
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Interface method that deletes the provided topic from the tree
    /// </summary>
    /// <param name="topic">The topic object to delete.</param>
    /// <param name="isRecursive">
    ///   Boolean indicator nothing whether to recurse through the topic's descendants and delete them as well. If set to false
    ///   (the default) and the topic has children, including any nested topics, an exception will be thrown.
    /// </param>
    /// <requires description="The topic to delete must be provided." exception="T:System.ArgumentNullException">topic != null</requires>
    /// <exception cref="ArgumentNullException">topic</exception>
    /// <exception cref="Exception">Failed to delete Topic <c>topic.Key</c> (<c>topic.Id</c>): <c>ex.Message</c></exception>
    public override void Delete(Topic topic, bool isRecursive = false) {
      _dataProvider.Delete(topic, isRecursive);
    }

  } //Class

} //Namespace
