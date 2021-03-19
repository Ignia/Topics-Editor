﻿/*==============================================================================================================================
| Author        Ignia, LLC
| Client        Ignia, LLC
| Project       Topics Library
\=============================================================================================================================*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using OnTopic.Attributes;
using OnTopic.Collections.Specialized;
using OnTopic.Internal.Diagnostics;
using OnTopic.Internal.Reflection;
using OnTopic.Lookup;
using OnTopic.Mapping.Annotations;
using OnTopic.Mapping.Internal;
using OnTopic.Models;
using OnTopic.Repositories;

namespace OnTopic.Mapping {

  /*============================================================================================================================
  | CLASS: TOPIC MAPPING SERVICE
  \---------------------------------------------------------------------------------------------------------------------------*/
  /// <summary>
  ///   Provides a concrete implementation of the <see cref="ITopicMappingService"/> for mapping <see cref="Topic"/> instances
  ///   to data transfer objects (such as view models) based on set conventions and attribute-based hints.
  /// </summary>
  public class TopicMappingService : ITopicMappingService {

    /*==========================================================================================================================
    | STATIC VARIABLES
    \-------------------------------------------------------------------------------------------------------------------------*/
    static readonly             MemberDispatcher                _typeCache                      = new();

    /*==========================================================================================================================
    | PRIVATE VARIABLES
    \-------------------------------------------------------------------------------------------------------------------------*/
    readonly                    ITopicRepository                _topicRepository;
    readonly                    ITypeLookupService              _typeLookupService;

    /*==========================================================================================================================
    | CONSTRUCTOR
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Establishes a new instance of a <see cref="TopicMappingService"/> with required dependencies.
    /// </summary>
    public TopicMappingService(ITopicRepository topicRepository, ITypeLookupService typeLookupService) {
      Contract.Requires(topicRepository, "An instance of an ITopicRepository is required.");
      Contract.Requires(typeLookupService, "An instance of an ITypeLookupService is required.");
      _topicRepository = topicRepository;
      _typeLookupService = typeLookupService;
    }

    /*==========================================================================================================================
    | METHOD: MAP (DYNAMIC)
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <inheritdoc />
    [return: NotNullIfNotNull("topic")]
    public async Task<object?> MapAsync(Topic? topic, AssociationTypes associations = AssociationTypes.All) =>
      await MapAsync(topic, associations, new()).ConfigureAwait(false);

    /// <summary>
    ///   Given a topic, will identify any View Models named, by convention, "{ContentType}TopicViewModel" and populate them
    ///   according to the rules of the mapping implementation.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Because the class is using reflection to determine the target View Models, the return type is <see cref="Object"/>.
    ///     These results may need to be cast to a specific type, depending on the context. That said, strongly typed views
    ///     should be able to cast the object to the appropriate View Model type. If the type of the View Model is known
    ///     upfront, and it is imperative that it be strongly typed, prefer <see cref="MapAsync{T}(Topic, AssociationTypes)"/>.
    ///   </para>
    ///   <para>
    ///     Because the target object is being dynamically constructed, it must implement a default constructor.
    ///   </para>
    ///   <para>
    ///     This internal version passes a private cache of mapped objects from this run. This helps prevent problems with
    ///     recursion in case <see cref="Topic"/> is referred to multiple times (e.g., a <c>Children</c> collection with
    ///     <see cref="IncludeAttribute"/> set to include <see cref="AssociationTypes.Parents"/>).
    ///   </para>
    /// </remarks>
    /// <param name="topic">The <see cref="Topic"/> entity to derive the data from.</param>
    /// <param name="associations">Determines what associations the mapping should include, if any.</param>
    /// <param name="cache">A cache to keep track of already-mapped object instances.</param>
    /// <param name="attributePrefix">The prefix to apply to the attributes.</param>
    /// <returns>An instance of the dynamically determined View Model with properties appropriately mapped.</returns>
    private async Task<object?> MapAsync(
      Topic?                    topic,
      AssociationTypes          associations,
      MappedTopicCache          cache,
      string?                   attributePrefix                 = null
    ) {

      /*------------------------------------------------------------------------------------------------------------------------
      | Validate input
      \-----------------------------------------------------------------------------------------------------------------------*/
      if (topic is null) {
        return null;
      }

      /*------------------------------------------------------------------------------------------------------------------------
      | Lookup type
      \-----------------------------------------------------------------------------------------------------------------------*/
      var viewModelType = _typeLookupService.Lookup($"{topic.ContentType}TopicViewModel", $"{topic.ContentType}ViewModel");

      if (viewModelType is null) {
        throw new InvalidTypeException(
          $"No class named '{topic.ContentType}TopicViewModel' could be located in any loaded assemblies. This is required " +
          $"to map the topic '{topic.GetUniqueKey()}'."
        );
      }

      /*------------------------------------------------------------------------------------------------------------------------
      | Perform mapping
      \-----------------------------------------------------------------------------------------------------------------------*/
      return await MapAsync(topic, viewModelType, associations, cache, attributePrefix).ConfigureAwait(false);

    }

    /// <summary>
    ///   Will map a given <paramref name="topic"/> to a given <paramref name="type"/>, according to the rules of the mapping
    ///   implementation.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Because the class is using reflection to determine the target View Models, the return type is <see cref="Object"/>.
    ///     These results may need to be cast to a specific type, depending on the context. That said, strongly-typed views
    ///     should be able to cast the object to the appropriate View Model type. If the type of the View Model is known
    ///     upfront, and it is imperative that it be strongly-typed, prefer <see cref="MapAsync{T}(Topic, AssociationTypes)"/>.
    ///   </para>
    ///   <para>
    ///     Because the target object is being dynamically constructed, it must implement a default constructor.
    ///   </para>
    ///   <para>
    ///     This internal version passes a private cache of mapped objects from this run. This helps prevent problems with
    ///     recursion in case <see cref="Topic"/> is referred to multiple times (e.g., a <c>Children</c> collection with
    ///     <see cref="IncludeAttribute"/> set to include <see cref="AssociationTypes.Parents"/>).
    ///   </para>
    /// </remarks>
    /// <param name="topic">The <see cref="Topic"/> entity to derive the data from.</param>
    /// <param name="type">The <see cref="Type"/> that should be used for the View Model.</param>
    /// <param name="associations">Determines what associations the mapping should include, if any.</param>
    /// <param name="cache">A cache to keep track of already-mapped object instances.</param>
    /// <param name="attributePrefix">The prefix to apply to the attributes.</param>
    /// <returns>An instance of the dynamically determined View Model with properties appropriately mapped.</returns>
    private async Task<object?> MapAsync(
      Topic?                    topic,
      Type                      type,
      AssociationTypes          associations,
      MappedTopicCache          cache,
      string?                   attributePrefix                 = null
    ) {

      /*------------------------------------------------------------------------------------------------------------------------
      | Validate input
      \-----------------------------------------------------------------------------------------------------------------------*/
      if (topic is null || type is null) {
        return null;
      }

      /*------------------------------------------------------------------------------------------------------------------------
      | Handle cached objects
      \-----------------------------------------------------------------------------------------------------------------------*/
      object? target;

      if (cache.TryGetValue(topic.Id, out var cacheEntry)) {
        target                  = cacheEntry.MappedTopic;
        if (cacheEntry.GetMissingAssociations(associations) == AssociationTypes.None) {
          return target;
        }
      }

      /*------------------------------------------------------------------------------------------------------------------------
      | Instantiate object
      \-----------------------------------------------------------------------------------------------------------------------*/
      else {

        target                  = Activator.CreateInstance(type);

        Contract.Assume(
          target,
          $"The target type '{type}' could not be properly constructed, as required to map the topic '{topic.GetUniqueKey()}'."
        );

      }

      /*------------------------------------------------------------------------------------------------------------------------
      | Provide mapping
      \-----------------------------------------------------------------------------------------------------------------------*/
      return await MapAsync(topic, target, associations, cache, attributePrefix).ConfigureAwait(false);

    }

    /*==========================================================================================================================
    | METHOD: MAP (T)
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <inheritdoc />
    public async Task<T?> MapAsync<T>(Topic? topic, AssociationTypes associations = AssociationTypes.All) where T : class, new() {
      if (typeof(Topic).IsAssignableFrom(typeof(T))) {
        return topic as T;
      }
      return (T?)await MapAsync(topic, new T(), associations).ConfigureAwait(false);
    }

    /*==========================================================================================================================
    | METHOD: MAP (OBJECTS)
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <inheritdoc />
    public async Task<object?> MapAsync(Topic? topic, object target, AssociationTypes associations = AssociationTypes.All) {
      Contract.Requires(target, nameof(target));
      return await MapAsync(topic, target, associations, new()).ConfigureAwait(false);
    }

    /// <summary>
    ///   Given a topic and an instance of a DTO, will populate the DTO according to the default mapping rules.
    /// </summary>
    /// <param name="topic">The <see cref="Topic"/> entity to derive the data from.</param>
    /// <param name="target">The target object to map the data to.</param>
    /// <param name="associations">Determines what associations the mapping should include, if any.</param>
    /// <param name="cache">A cache to keep track of already-mapped object instances.</param>
    /// <param name="attributePrefix">The prefix to apply to the attributes.</param>
    /// <remarks>
    ///   This internal version passes a private cache of mapped objects from this run. This helps prevent problems with
    ///   recursion in case <see cref="Topic"/> is referred to multiple times (e.g., a <c>Children</c> collection with <see cref
    ///   ="IncludeAttribute"/> set to include <see cref="AssociationTypes.Parents"/>).
    /// </remarks>
    /// <returns>
    ///   The target view model with the properties appropriately mapped.
    /// </returns>
    private async Task<object> MapAsync(
      Topic?                    topic,
      object                    target,
      AssociationTypes          associations,
      MappedTopicCache          cache,
      string?                   attributePrefix                 = null
    ) {

      /*------------------------------------------------------------------------------------------------------------------------
      | Validate input
      \-----------------------------------------------------------------------------------------------------------------------*/
      if (topic is null) {
        return target;
      }

      /*------------------------------------------------------------------------------------------------------------------------
      | Handle topics
      \-----------------------------------------------------------------------------------------------------------------------*/
      if (typeof(Topic).IsAssignableFrom(target.GetType())) {
        return topic;
      }

      /*------------------------------------------------------------------------------------------------------------------------
      | Handle cached objects
      >-------------------------------------------------------------------------------------------------------------------------
      | If the cache contains an entry, check to make sure it includes all of the requested associations. If it does, return it.
      | If it doesn't, determine the missing associations and request to have those mapped.
      \-----------------------------------------------------------------------------------------------------------------------*/
      if (cache.TryGetValue(topic.Id, out var cacheEntry)) {
        associations            = cacheEntry.GetMissingAssociations(associations);
        target                  = cacheEntry.MappedTopic;
        if (associations == AssociationTypes.None) {
          return cacheEntry.MappedTopic;
        }
        cacheEntry.AddMissingAssociations(associations);
      }
      else if (!topic.IsNew) {
        cache.GetOrAdd(
          topic.Id,
          new MappedTopicCacheEntry() {
            MappedTopic         = target,
            Associations        = associations
          }
        );
      }

      /*------------------------------------------------------------------------------------------------------------------------
      | Loop through properties, mapping each one
      \-----------------------------------------------------------------------------------------------------------------------*/
      var taskQueue = new List<Task>();
      foreach (var property in _typeCache.GetMembers<PropertyInfo>(target.GetType())) {
        taskQueue.Add(SetPropertyAsync(topic, target, associations, property, cache, attributePrefix, cacheEntry != null));
      }
      await Task.WhenAll(taskQueue.ToArray()).ConfigureAwait(false);

      /*------------------------------------------------------------------------------------------------------------------------
      | Return result
      \-----------------------------------------------------------------------------------------------------------------------*/
      return target;

    }

    /*==========================================================================================================================
    | PRIVATE: SET PROPERTY (ASYNC)
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Helper function that evaluates each property on the target object and attempts to retrieve a value from the source
    ///   <see cref="Topic"/> based on predetermined conventions.
    /// </summary>
    /// <param name="source">The <see cref="Topic"/> entity to derive the data from.</param>
    /// <param name="target">The target object to map the data to.</param>
    /// <param name="associations">Determines what associations the mapping should include, if any.</param>
    /// <param name="property">Information related to the current property.</param>
    /// <param name="cache">A cache to keep track of already-mapped object instances.</param>
    /// <param name="attributePrefix">The prefix to apply to the attributes.</param>
    /// <param name="mapAssociationsOnly">Determines if properties not associated with associations should be mapped.</param>
    private async Task SetPropertyAsync(
      Topic                     source,
      object                    target,
      AssociationTypes          associations,
      PropertyInfo              property,
      MappedTopicCache          cache,
      string?                   attributePrefix                 = null,
      bool                      mapAssociationsOnly             = false
    ) {

      /*------------------------------------------------------------------------------------------------------------------------
      | Validate parameters
      \-----------------------------------------------------------------------------------------------------------------------*/
      Contract.Requires(source, nameof(source));
      Contract.Requires(target, nameof(target));
      Contract.Requires(associations, nameof(associations));
      Contract.Requires(property, nameof(property));
      Contract.Requires(cache, nameof(cache));

      /*------------------------------------------------------------------------------------------------------------------------
      | Establish per-property variables
      \-----------------------------------------------------------------------------------------------------------------------*/
      var configuration         = new PropertyConfiguration(property, attributePrefix);
      var topicReferenceId      = source.Attributes.GetInteger($"{configuration.AttributeKey}Id", 0);
      var topicReference        = source.References.GetValue(configuration.AttributeKey);

      if (topicReferenceId == 0 && configuration.AttributeKey.EndsWith("Id", StringComparison.OrdinalIgnoreCase)) {
        topicReferenceId        = source.Attributes.GetInteger(configuration.AttributeKey, 0);
      }

      /*------------------------------------------------------------------------------------------------------------------------
      | Assign default value
      \-----------------------------------------------------------------------------------------------------------------------*/
      if (!mapAssociationsOnly && configuration.DefaultValue is not null) {
        property.SetValue(target, configuration.DefaultValue);
      }

      /*------------------------------------------------------------------------------------------------------------------------
      | Handle by type, attribute
      \-----------------------------------------------------------------------------------------------------------------------*/
      if (configuration.DisableMapping) {
        return;
      }
      else if (SetCompatibleProperty(source, target, configuration)) {
        //Performed 1:1 mapping between source and target
      }
      else if (!mapAssociationsOnly && _typeCache.HasSettableProperty(target.GetType(), property.Name)) {
        SetScalarValue(source, target, configuration);
      }
      else if (typeof(IList).IsAssignableFrom(property.PropertyType)) {
        await SetCollectionValueAsync(source, target, associations, configuration, cache).ConfigureAwait(false);
      }
      else if (configuration.AttributeKey is "Parent" && associations.HasFlag(AssociationTypes.Parents)) {
        if (source.Parent is not null) {
          await SetTopicReferenceAsync(source.Parent, target, configuration, cache).ConfigureAwait(false);
        }
      }
      else if (
        topicReference is not null &&
        associations.HasFlag(AssociationTypes.References)
      ) {
        await SetTopicReferenceAsync(topicReference, target, configuration, cache).ConfigureAwait(false);
      }
      else if (topicReferenceId > 0 && associations.HasFlag(AssociationTypes.References)) {
        topicReference = _topicRepository.Load(topicReferenceId, source);
        if (topicReference is not null) {
          await SetTopicReferenceAsync(topicReference, target, configuration, cache).ConfigureAwait(false);
        }
      }
      else if (configuration.MapToParent) {
        var targetProperty = property.GetValue(target);
        if (targetProperty is not null) {
          await MapAsync(
            source,
            targetProperty,
            associations,
            cache,
            configuration.AttributePrefix
          ).ConfigureAwait(false);
        }

      }

      /*------------------------------------------------------------------------------------------------------------------------
      | Validate fields
      \-----------------------------------------------------------------------------------------------------------------------*/
      configuration.Validate(target);

    }

    /*==========================================================================================================================
    | PRIVATE: SET SCALAR VALUE
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Sets a scalar property on a target DTO.
    /// </summary>
    /// <remarks>
    ///   Assuming the <paramref name="configuration"/>'s <see cref="PropertyConfiguration.Property"/> is of the type <see
    ///   cref="String"/>, <see cref="Boolean"/>, <see cref="Int32"/>, or <see cref="DateTime"/>, the <see
    ///   cref="SetScalarValue(Topic,Object, PropertyConfiguration)"/> method will attempt to set the property on the <paramref
    ///   name="target"/> based on, in order, the <paramref name="source"/>'s <c>Get{Property}()</c> method, <c>{Property}</c>
    ///   property, and, finally, its <see cref="Topic.Attributes"/> collection (using <see cref="TrackedRecordCollection{TItem,
    ///   TValue, TAttribute}.GetValue(String, Boolean)"/>). If the property is not of a settable type, or the source value
    ///   cannot be identified on the <paramref name="source"/>, then the property is not set.
    /// </remarks>
    /// <param name="source">The source <see cref="Topic"/> from which to pull the value.</param>
    /// <param name="target">The target DTO on which to set the property value.</param>
    /// <param name="configuration">The <see cref="PropertyConfiguration"/> with details about the property's attributes.</param>
    /// <autogeneratedoc />
    private static void SetScalarValue(Topic source, object target, PropertyConfiguration configuration) {

      /*------------------------------------------------------------------------------------------------------------------------
      | Validate parameters
      \-----------------------------------------------------------------------------------------------------------------------*/
      Contract.Requires(source, nameof(source));
      Contract.Requires(target, nameof(target));
      Contract.Requires(configuration, nameof(configuration));

      /*------------------------------------------------------------------------------------------------------------------------
      | Escape clause if preconditions are not met
      \-----------------------------------------------------------------------------------------------------------------------*/
      if (!_typeCache.HasSettableProperty(target.GetType(), configuration.Property.Name)) {
        return;
      }

      /*------------------------------------------------------------------------------------------------------------------------
      | Attempt to retrieve value from topic.Get{Property}()
      \-----------------------------------------------------------------------------------------------------------------------*/
      var attributeValue = _typeCache.GetMethodValue(source, $"Get{configuration.AttributeKey}")?.ToString();

      /*------------------------------------------------------------------------------------------------------------------------
      | Attempt to retrieve value from topic.{Property}
      \-----------------------------------------------------------------------------------------------------------------------*/
      if (String.IsNullOrEmpty(attributeValue)) {
        attributeValue = _typeCache.GetPropertyValue(source, configuration.AttributeKey)?.ToString();
      }

      /*------------------------------------------------------------------------------------------------------------------------
      | Otherwise, attempt to retrieve value from topic.Attributes.GetValue({Property})
      \-----------------------------------------------------------------------------------------------------------------------*/
      if (String.IsNullOrEmpty(attributeValue)) {
        attributeValue = source.Attributes.GetValue(
          configuration.AttributeKey,
          configuration.DefaultValue?.ToString(),
          configuration.InheritValue
        );
      }

      /*------------------------------------------------------------------------------------------------------------------------
      | Assuming a value was retrieved, set it
      \-----------------------------------------------------------------------------------------------------------------------*/
      if (attributeValue is not null) {
        _typeCache.SetPropertyValue(target, configuration.Property.Name, attributeValue);
      }

    }

    /*==========================================================================================================================
    | PRIVATE: SET COLLECTION VALUE
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Given a collection property, identifies a source collection, maps the values to DTOs, and attempts to add them to the
    ///   target collection.
    /// </summary>
    /// <remarks>
    ///   Given a collection <paramref name="configuration"/> on a <paramref name="target"/> DTO, attempts to identify a source
    ///   collection on the <paramref name="source"/>. Collections can be mapped to <see cref="Topic.Children"/>, <see
    ///   cref="Topic.Relationships"/>, <see cref="Topic.IncomingRelationships"/> or to a nested topic (which will be part of
    ///   <see cref="Topic.Children"/>). By default, <see cref="TopicMappingService"/> will attempt to map based on the
    ///   property name, though this behavior can be modified using the <paramref name="configuration"/>, based on annotations
    ///   on the <paramref name="target"/> DTO.
    /// </remarks>
    /// <param name="source">The source <see cref="Topic"/> from which to pull the value.</param>
    /// <param name="target">The target DTO on which to set the property value.</param>
    /// <param name="associations">Determines what associations the mapping should include, if any.</param>
    /// <param name="configuration">
    ///   The <see cref="PropertyConfiguration"/> with details about the property's attributes.
    /// </param>
    /// <param name="cache">A cache to keep track of already-mapped object instances.</param>
    private async Task SetCollectionValueAsync(
      Topic                     source,
      object                    target,
      AssociationTypes          associations,
      PropertyConfiguration     configuration,
      MappedTopicCache          cache
    ) {

      /*------------------------------------------------------------------------------------------------------------------------
      | Validate parameters
      \-----------------------------------------------------------------------------------------------------------------------*/
      Contract.Requires(source, nameof(source));
      Contract.Requires(associations, nameof(associations));
      Contract.Requires(configuration, nameof(configuration));
      Contract.Requires(cache, nameof(cache));

      /*------------------------------------------------------------------------------------------------------------------------
      | Escape clause if preconditions are not met
      \-----------------------------------------------------------------------------------------------------------------------*/
      if (!typeof(IList).IsAssignableFrom(configuration.Property.PropertyType)) return;

      /*------------------------------------------------------------------------------------------------------------------------
      | Ensure target list is created
      \-----------------------------------------------------------------------------------------------------------------------*/
      var targetList = (IList?)configuration.Property.GetValue(target, null);
      if (targetList is null) {
        targetList = (IList?)Activator.CreateInstance(configuration.Property.PropertyType);
        configuration.Property.SetValue(target, targetList);
      }

      Contract.Assume(
        targetList,
        $"The target list type, '{configuration.Property.PropertyType}', could not be properly constructed, as required to " +
        $"map the '{configuration.Property.Name}' property on the '{target?.GetType().Name}' object."
      );

      /*------------------------------------------------------------------------------------------------------------------------
      | Establish source collection to store topics to be mapped
      \-----------------------------------------------------------------------------------------------------------------------*/
      var sourceList = GetSourceCollection(source, associations, configuration);

      /*------------------------------------------------------------------------------------------------------------------------
      | Validate that source collection was identified
      \-----------------------------------------------------------------------------------------------------------------------*/
      if (sourceList is null) return;

      /*------------------------------------------------------------------------------------------------------------------------
      | Map the topics from the source collection, and add them to the target collection
      \-----------------------------------------------------------------------------------------------------------------------*/
      await PopulateTargetCollectionAsync(sourceList, targetList, configuration, cache).ConfigureAwait(false);

    }

    /*==========================================================================================================================
    | PRIVATE: GET SOURCE COLLECTION
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Given a source topic and a property configuration, attempts to identify a source collection that maps to the property.
    /// </summary>
    /// <remarks>
    ///   Given a collection <paramref name="configuration"/> on a target DTO, attempts to identify a source collection on the
    ///   <paramref name="source"/>. Collections can be mapped to <see cref="Topic.Children"/>, <see
    ///   cref="Topic.Relationships"/>, <see cref="Topic.IncomingRelationships"/> or to a nested topic (which will be part of
    ///   <see cref="Topic.Children"/>). By default, <see cref="TopicMappingService"/> will attempt to map based on the
    ///   property name, though this behavior can be modified using the <paramref name="configuration"/>, based on annotations
    ///   on the target DTO.
    /// </remarks>
    /// <param name="source">The source <see cref="Topic"/> from which to pull the value.</param>
    /// <param name="associations">Determines what associations the mapping should include, if any.</param>
    /// <param name="configuration">
    ///   The <see cref="PropertyConfiguration"/> with details about the property's attributes.
    /// </param>
    private IList<Topic> GetSourceCollection(Topic source, AssociationTypes associations, PropertyConfiguration configuration) {

      /*------------------------------------------------------------------------------------------------------------------------
      | Validate parameters
      \-----------------------------------------------------------------------------------------------------------------------*/
      Contract.Requires(source, nameof(source));
      Contract.Requires(associations, nameof(associations));
      Contract.Requires(configuration, nameof(configuration));

      /*------------------------------------------------------------------------------------------------------------------------
      | Establish source collection to store topics to be mapped
      \-----------------------------------------------------------------------------------------------------------------------*/
      var                       listSource                      = (IList<Topic>)Array.Empty<Topic>();
      var                       collectionKey                   = configuration.CollectionKey;
      var                       collectionType                  = configuration.CollectionType;

      /*------------------------------------------------------------------------------------------------------------------------
      | Handle children
      \-----------------------------------------------------------------------------------------------------------------------*/
      listSource = getCollection(
        CollectionType.Children,
        s => true,
        () => source.Children.ToList()
      );

      /*------------------------------------------------------------------------------------------------------------------------
      | Handle (outgoing) relationships
      \-----------------------------------------------------------------------------------------------------------------------*/
      listSource = getCollection(
        CollectionType.Relationship,
        source.Relationships.Contains,
        () => source.Relationships.GetValues(collectionKey)
      );

      /*------------------------------------------------------------------------------------------------------------------------
      | Handle nested topics, or children corresponding to the property name
      \-----------------------------------------------------------------------------------------------------------------------*/
      listSource = getCollection(
        CollectionType.NestedTopics,
        source.Children.Contains,
        () => source.Children[collectionKey].Children
      );

      /*------------------------------------------------------------------------------------------------------------------------
      | Handle (incoming) relationships
      \-----------------------------------------------------------------------------------------------------------------------*/
      listSource = getCollection(
        CollectionType.IncomingRelationship,
        source.IncomingRelationships.Contains,
        () => source.IncomingRelationships.GetValues(collectionKey)
      );

      /*------------------------------------------------------------------------------------------------------------------------
      | Handle other strongly typed source collections
      \-----------------------------------------------------------------------------------------------------------------------*/
      //The following allows a target collection to be mapped to an IList<Topic> source collection. This is valuable for custom,
      //curated collections defined on e.g. derivatives of Topic, but which don't otherwise map to a specific collection type.
      //For example, the ContentTypeDescriptor's AttributeDescriptors collection, which provides a rollup of
      //AttributeDescriptors from the current ContentTypeDescriptor, as well as all of its ascendents.
      if (listSource.Count == 0) {
        var sourceProperty = _typeCache.GetMember<PropertyInfo>(source.GetType(), configuration.AttributeKey);
        if (
          sourceProperty?.GetValue(source) is IList sourcePropertyValue &&
          sourcePropertyValue.Count > 0 &&
          typeof(Topic).IsAssignableFrom(sourcePropertyValue[0]?.GetType())
        ) {
          listSource = getCollection(
            CollectionType.MappedCollection,
            s => true,
            () => sourcePropertyValue.Cast<Topic>().ToList()
          );
        }
      }

      /*------------------------------------------------------------------------------------------------------------------------
      | Handle Metadata relationship
      \-----------------------------------------------------------------------------------------------------------------------*/
      if (listSource.Count == 0 && !String.IsNullOrWhiteSpace(configuration.MetadataKey)) {
        var metadataKey = $"Root:Configuration:Metadata:{configuration.MetadataKey}:LookupList";
        var metadataParent = _topicRepository.Load(metadataKey, source);
        if (metadataParent is not null) {
          listSource = metadataParent.Children.ToList();
        }
      }

      /*------------------------------------------------------------------------------------------------------------------------
      | Handle flattening of children
      \-----------------------------------------------------------------------------------------------------------------------*/
      if (configuration.FlattenChildren) {
        var flattenedList = new List<Topic>();
        listSource.ToList().ForEach(t => FlattenTopicGraph(t, flattenedList));
        listSource = flattenedList;
      }

      return listSource;

      /*------------------------------------------------------------------------------------------------------------------------
      | Provide local function for evaluating current collection
      \-----------------------------------------------------------------------------------------------------------------------*/
      IList<Topic> getCollection(CollectionType collection, Func<string, bool> contains, Func<IList<Topic>> getTopics) {
        var targetAssociations = AssociationMap.Mappings[collection];
        var preconditionsMet    =
          listSource.Count == 0 &&
          (collectionType is CollectionType.Any || collectionType.Equals(collection)) &&
          (collectionType is CollectionType.Children || collection is not CollectionType.Children) &&
          (targetAssociations is AssociationTypes.None || associations.HasFlag(targetAssociations)) &&
          contains(configuration.CollectionKey);
        return preconditionsMet? getTopics() : listSource;
      }

    }

    /*==========================================================================================================================
    | PRIVATE: POPULATE TARGET COLLECTION
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Given a source list, will populate a target list based on the configured behavior of the target property.
    /// </summary>
    /// <param name="sourceList">The <see cref="IList{Topic}"/> to pull the source <see cref="Topic"/> objects from.</param>
    /// <param name="targetList">The target <see cref="IList"/> to add the mapped <see cref="Topic"/> objects to.</param>
    /// <param name="configuration">
    ///   The <see cref="PropertyConfiguration"/> with details about the property's attributes.
    /// </param>
    /// <param name="cache">A cache to keep track of already-mapped object instances.</param>
    private async Task PopulateTargetCollectionAsync(
      IList<Topic>              sourceList,
      IList                     targetList,
      PropertyConfiguration     configuration,
      MappedTopicCache          cache
    ) {

      /*------------------------------------------------------------------------------------------------------------------------
      | Validate parameters
      \-----------------------------------------------------------------------------------------------------------------------*/
      Contract.Requires(sourceList, nameof(sourceList));
      Contract.Requires(targetList, nameof(targetList));
      Contract.Requires(configuration, nameof(configuration));
      Contract.Requires(cache, nameof(cache));

      /*------------------------------------------------------------------------------------------------------------------------
      | Determine the type of item in the list
      \-----------------------------------------------------------------------------------------------------------------------*/
      var listType = typeof(ITopicViewModel);
      foreach (var type in configuration.Property.PropertyType.GetInterfaces()) {
        if (type.IsGenericType && typeof(IList<>) == type.GetGenericTypeDefinition()) {
          //Uses last argument in case it's a KeyedCollection; in that case, we want the TItem type
          listType = type.GetGenericArguments().Last();
        }
      }

      /*------------------------------------------------------------------------------------------------------------------------
      | Queue up mapping tasks
      \-----------------------------------------------------------------------------------------------------------------------*/
      var taskQueue = new List<Task<object?>>();

      foreach (var childTopic in sourceList) {

        //Ensure the source topic matches any [FilterByAttribute()] settings
        if (!configuration.SatisfiesAttributeFilters(childTopic)) {
          continue;
        }

        if (
          configuration.ContentTypeFilter is not null &&
          !childTopic.ContentType.Equals(configuration.ContentTypeFilter, StringComparison.OrdinalIgnoreCase)
        ) {
          continue;
        }

        //Skip nested topics; those should be explicitly mapped to their own collection or topic reference
        if (childTopic.ContentType.Equals("List", StringComparison.OrdinalIgnoreCase)) {
          continue;
        }

        //Ensure the source topic isn't disabled; disabled topics should never be returned to the presentation layer unless
        //explicitly requested by a top-level request.
        if (childTopic.IsDisabled) {
          continue;
        }

        //Map child topic to target DTO
        var childDto = (object)childTopic;
        if (!typeof(Topic).IsAssignableFrom(listType)) {
          taskQueue.Add(MapAsync(childTopic, configuration.IncludeAssociations, cache));
        }
        else {
          AddToList(childDto);
        }

      }

      /*------------------------------------------------------------------------------------------------------------------------
      | Process mapping tasks
      \-----------------------------------------------------------------------------------------------------------------------*/
      while (taskQueue.Count > 0) {
        var dtoTask             = await Task.WhenAny(taskQueue).ConfigureAwait(false);
        var dto                 = await dtoTask.ConfigureAwait(false);
        taskQueue.Remove(dtoTask);
        if (dto is not null) {
          AddToList(dto);
        }
      }

      /*------------------------------------------------------------------------------------------------------------------------
      | Function: Add to List
      \-----------------------------------------------------------------------------------------------------------------------*/
      void AddToList(object dto) {
        if (dto is not null && listType.IsAssignableFrom(dto.GetType())) {
          try {
            targetList.Add(dto);
          }
          catch (ArgumentException) {
            //Ignore exceptions caused by duplicate keys, in case the IList represents a keyed collection
            //We would defensively check for this, except IList doesn't provide a suitable method to do so
          }
        }
      }

    }

    /*==========================================================================================================================
    | PRIVATE: SET TOPIC REFERENCE
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Given a reference to an external topic, attempts to match it to a matching property.
    /// </summary>
    /// <param name="source">The source <see cref="Topic"/> from which to pull the value.</param>
    /// <param name="target">The target DTO on which to set the property value.</param>
    /// <param name="configuration">
    ///   The <see cref="PropertyConfiguration"/> with details about the property's attributes.
    /// </param>
    /// <param name="cache">A cache to keep track of already-mapped object instances.</param>
    private async Task SetTopicReferenceAsync(
      Topic                     source,
      object                    target,
      PropertyConfiguration     configuration,
      MappedTopicCache          cache
    ) {

      /*------------------------------------------------------------------------------------------------------------------------
      | Validate parameters
      \-----------------------------------------------------------------------------------------------------------------------*/
      Contract.Requires(source, nameof(source));
      Contract.Requires(target, nameof(target));
      Contract.Requires(configuration, nameof(configuration));
      Contract.Requires(cache, nameof(cache));

      /*------------------------------------------------------------------------------------------------------------------------
      | Bypass disabled topics
      \-----------------------------------------------------------------------------------------------------------------------*/
      //Ensure the source topic isn't disabled; disabled topics should never be returned to the presentation layer unless
      //explicitly requested by a top-level request.
      if (source.IsDisabled) {
        return;
      }

      /*------------------------------------------------------------------------------------------------------------------------
      | Map referenced topic
      \-----------------------------------------------------------------------------------------------------------------------*/
      var topicDto = (object?)null;
      try {
        topicDto = await MapAsync(source, configuration.IncludeAssociations, cache).ConfigureAwait(false);
      }
      catch (InvalidTypeException) {
        //Disregard errors caused by unmapped view models; those are functionally equivalent to IsAssignableFrom() mismatches
      }
      if (topicDto is not null && configuration.Property.PropertyType.IsAssignableFrom(topicDto.GetType())) {
        configuration.Property.SetValue(target, topicDto);
      }
    }

    /*==========================================================================================================================
    | PRIVATE: FLATTEN TOPIC GRAPH
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Helper function recursively iterates through children and adds each to a collection.
    /// </summary>
    /// <param name="source">The <see cref="Topic"/> entity pull the data from.</param>
    /// <param name="targetList">The list of <see cref="Topic"/> instances to add each child to.</param>
    private IList<Topic> FlattenTopicGraph(Topic source, IList<Topic> targetList) {

      /*------------------------------------------------------------------------------------------------------------------------
      | Validate parameters
      \-----------------------------------------------------------------------------------------------------------------------*/
      Contract.Requires(source, nameof(source));
      Contract.Requires(targetList, nameof(targetList));

      /*------------------------------------------------------------------------------------------------------------------------
      | Validate source properties
      \-----------------------------------------------------------------------------------------------------------------------*/
      if (source.IsDisabled) return targetList;
      if (source.ContentType is "List") return targetList;

      /*------------------------------------------------------------------------------------------------------------------------
      | Merge source list into target list
      \-----------------------------------------------------------------------------------------------------------------------*/
      targetList.Add(source);
      source.Children.ToList().ForEach(t => FlattenTopicGraph(t, targetList));
      return targetList;

    }

    /*==========================================================================================================================
    | PRIVATE: SET COMPATIBLE PROPERTY
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Sets a property on the target view model to a compatible value on the source object.
    /// </summary>
    /// <remarks>
    ///   Even if the property values can't be set by the <see cref="MemberDispatcher"/>, properties should be settable
    ///   assuming the source and target types are compatible. In this case, <see cref="TopicMappingService"/> needn't know
    ///   anything about the property type as it doesn't need to do a conversion; it can just do a one-to-one mapping.
    /// </remarks>
    /// <param name="source">The source <see cref="Topic"/> from which to pull the value.</param>
    /// <param name="target">The target DTO on which to set the property value.</param>
    /// <param name="configuration">The <see cref="PropertyConfiguration"/> with details about the property's attributes.</param>
    /// <autogeneratedoc />
    private static bool SetCompatibleProperty(Topic source, object target, PropertyConfiguration configuration) {

      /*------------------------------------------------------------------------------------------------------------------------
      | Validate parameters
      \-----------------------------------------------------------------------------------------------------------------------*/
      Contract.Requires(source, nameof(source));
      Contract.Requires(target, nameof(target));
      Contract.Requires(configuration, nameof(configuration));

      /*------------------------------------------------------------------------------------------------------------------------
      | Attempt to retrieve value from topic.{Property}
      \-----------------------------------------------------------------------------------------------------------------------*/
      var sourceProperty = _typeCache.GetMember<PropertyInfo>(source.GetType(), configuration.AttributeKey);

      /*------------------------------------------------------------------------------------------------------------------------
      | Escape clause if preconditions are not met
      \-----------------------------------------------------------------------------------------------------------------------*/
      if (sourceProperty is null || !configuration.Property.PropertyType.IsAssignableFrom(sourceProperty.PropertyType)) {
        return false;
      }

      /*------------------------------------------------------------------------------------------------------------------------
      | Assuming a value was retrieved, set it
      \-----------------------------------------------------------------------------------------------------------------------*/
      configuration.Property.SetValue(target, sourceProperty.GetValue(source));

      return true;

    }

  } //Class
} //Namespace