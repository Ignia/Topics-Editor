﻿/*==============================================================================================================================
| Author        Ignia, LLC
| Client        Ignia, LLC
| Project       Topics Library
\=============================================================================================================================*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Ignia.Topics.Collections;
using Ignia.Topics.Internal.Diagnostics;
using Ignia.Topics.Internal.Mapping;
using Ignia.Topics.Internal.Reflection;
using Ignia.Topics.Metadata;
using Ignia.Topics.Models;
using Ignia.Topics.Repositories;
using System.ComponentModel;

namespace Ignia.Topics.Mapping {

  /*============================================================================================================================
  | CLASS: REVERSE TOPIC MAPPING SERVICE
  \---------------------------------------------------------------------------------------------------------------------------*/
  /// <inheritdoc/>
  public class ReverseTopicMappingService : IReverseTopicMappingService {

    /*==========================================================================================================================
    | STATIC VARIABLES
    \-------------------------------------------------------------------------------------------------------------------------*/
    static readonly             TypeMemberInfoCollection        _typeCache                      = new TypeMemberInfoCollection();

    /*==========================================================================================================================
    | PRIVATE VARIABLES
    \-------------------------------------------------------------------------------------------------------------------------*/
    readonly                    ITopicRepository                _topicRepository                = null;
    readonly                    ITypeLookupService              _typeLookupService              = null;
    readonly                    ContentTypeDescriptorCollection _contentTypeDescriptors         = null;

    /*==========================================================================================================================
    | CONSTRUCTOR
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Establishes a new instance of a <see cref="ReverseTopicMappingService"/> with required dependencies.
    /// </summary>
    public ReverseTopicMappingService(ITopicRepository topicRepository, ITypeLookupService typeLookupService) {

      /*----------------------------------------------------------------------------------------------------------------------
      | Validate dependencies
      \---------------------------------------------------------------------------------------------------------------------*/
      Contract.Requires<ArgumentNullException>(topicRepository != null, "An instance of an ITopicRepository is required.");
      Contract.Requires<ArgumentNullException>(typeLookupService != null, "An instance of an ITypeLookupService is required.");

      /*----------------------------------------------------------------------------------------------------------------------
      | Set dependencies
      \---------------------------------------------------------------------------------------------------------------------*/
      _topicRepository          = topicRepository;
      _typeLookupService        = typeLookupService;
      _contentTypeDescriptors   = topicRepository.GetContentTypeDescriptors();

    }

    /*==========================================================================================================================
    | METHOD: MAP (ASYNC)
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <inheritdoc/>
    public async Task<Topic> MapAsync(ITopicBindingModel source) {

      /*----------------------------------------------------------------------------------------------------------------------
      | Handle null source
      \---------------------------------------------------------------------------------------------------------------------*/
      if (source == null) return null;

      /*----------------------------------------------------------------------------------------------------------------------
      | Instantiate target
      \---------------------------------------------------------------------------------------------------------------------*/
      var topic = TopicFactory.Create(source.Key, source.ContentType);

      /*----------------------------------------------------------------------------------------------------------------------
      | Provide mapping
      \---------------------------------------------------------------------------------------------------------------------*/
      return await MapAsync(source, topic).ConfigureAwait(false);

    }

    /*==========================================================================================================================
    | METHOD: MAP (T)
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <inheritdoc/>
    public async Task<T> MapAsync<T>(ITopicBindingModel source) where T : Topic {
      return (T)await MapAsync(
        source,
        TopicFactory.Create(source.Key, source.ContentType)
      ).ConfigureAwait(false);
    }

    /*==========================================================================================================================
    | METHOD: MAP (TOPIC)
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <inheritdoc/>
    public async Task<Topic> MapAsync(ITopicBindingModel source, Topic target) {

      /*------------------------------------------------------------------------------------------------------------------------
      | Validate input
      \-----------------------------------------------------------------------------------------------------------------------*/
      if (source == null) {
        return target;
      }

      //Ensure the content type is valid
      if (!_contentTypeDescriptors.Contains(source.ContentType)) {
        throw new InvalidEnumArgumentException(
          $"The {nameof(source)} object (with the key '{source.Key}') has a content type of '{source.ContentType}'. There " +
          $"no matching content type in the ITopicRepository provided. This suggests that the binding model is invalid. If " +
          $"this is expected—e.g., if the content type is being added as part of this operation—then it needs to be added " +
          $"to the same ITopicRepository instance prior to creating any instances of it."
        );
      }

      //Ensure the content types match
      if (source.ContentType != target.ContentType) {
        throw new InvalidEnumArgumentException(
          $"The {nameof(source)} object (with the key '{source.Key}') has a content type of '{source.ContentType}', while " +
          $"the {nameof(target)} object (with the key '{source.Key}') has a content type of '{target.ContentType}'. It is not" +
          $"permitted to change the topic's content type during a mapping operation, as this interferes with the validation. If" +
          $"this is by design, change the content type on the target topic prior to invoking MapAsync()."
        );
      }

      //Ensure the keys match
      if (source.Key != target.Key && !String.IsNullOrEmpty(source.Key)) {
        throw new InvalidEnumArgumentException(
          $"The {nameof(source)} object has a key of '{source.Key}', while the {nameof(target)} object has a key of " +
          $"'{target.Key}'. It is not permitted to change the topic'key during a mapping operation, as this suggests in " +
          $"invalid target. If this is by design, change the key on the target topic prior to invoking MapAsync()."
        );
      }

      /*------------------------------------------------------------------------------------------------------------------------
      | Validate model
      \-----------------------------------------------------------------------------------------------------------------------*/
      var properties = _typeCache.GetMembers<PropertyInfo>(source.GetType());

      BindingModelValidator.ValidateModel(source.GetType(), properties, _contentTypeDescriptors.GetTopic(target.ContentType));

      /*------------------------------------------------------------------------------------------------------------------------
      | Loop through properties, mapping each one
      \-----------------------------------------------------------------------------------------------------------------------*/
      var taskQueue = new List<Task>();
      foreach (var property in properties) {
        taskQueue.Add(SetPropertyAsync(source, target, property));
      }
      await Task.WhenAll(taskQueue.ToArray()).ConfigureAwait(false);

      /*------------------------------------------------------------------------------------------------------------------------
      | Return result
      \-----------------------------------------------------------------------------------------------------------------------*/
      return target;

    }

    /*==========================================================================================================================
    | PROTECTED: SET PROPERTY (ASYNC)
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Helper function that evaluates each property on the source <see cref="ITopicBindingModel"/> and then attempts to
    ///   locate and set the associated attribute, collection, or property on the target <see cref="Topic"/> based on
    ///   predetermined conventions.
    /// </summary>
    /// <param name="source">
    ///   The binding model from which to derive the data. Must inherit from <see cref="ITopicBindingModel"/>.
    /// </param>
    /// <param name="target">The <see cref="Topic"/> entity to map the data to.</param>
    /// <param name="property">Information related to the current property.</param>
    protected async Task SetPropertyAsync(
      ITopicBindingModel        source,
      Topic                     target,
      PropertyInfo              property
    ) {

      /*------------------------------------------------------------------------------------------------------------------------
      | Establish per-property variables
      \-----------------------------------------------------------------------------------------------------------------------*/
      var configuration         = new PropertyConfiguration(property);
      var contentType           = _contentTypeDescriptors[source.ContentType];
      var attributeType         = contentType.AttributeDescriptors.GetTopic(configuration.AttributeKey);

      /*------------------------------------------------------------------------------------------------------------------------
      | Validate fields
      \-----------------------------------------------------------------------------------------------------------------------*/
      configuration.Validate(source);

      /*------------------------------------------------------------------------------------------------------------------------
      | Handle by type, attribute
      \-----------------------------------------------------------------------------------------------------------------------*/
      if (_typeCache.HasSettableProperty(source.GetType(), property.Name) && attributeType.ModelType == ModelType.ScalarValue) {
        SetScalarValue(source, target, configuration);
      }
      else if (typeof(IList).IsAssignableFrom(property.PropertyType) && attributeType.ModelType == ModelType.Relationship) {
      }
      else if (typeof(IList).IsAssignableFrom(property.PropertyType) && attributeType.ModelType == ModelType.NestedTopic) {
        var topicReference = _topicRepository.Load(((IRelatedTopicBindingModel)property.GetValue(source)).UniqueKey);
        //#### TODO JJC20190221: Should this enforce the appending of an ID to maintain the convention? Or assume the
        //developer knows to set an alias, or is deliberately breaking the convention?
        target.Attributes.SetInteger(configuration.AttributeKey, topicReference.Id);
      }

    }

    /*==========================================================================================================================
    | PROTECTED: SET SCALAR VALUE
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Sets an attribute on the target <see cref="Topic"/> with a scalar value from the source binding model.
    /// </summary>
    /// <remarks>
    ///   Assuming the <paramref name="configuration"/>'s <see cref="PropertyConfiguration.Property"/> is of the type <see
    ///   cref="String"/>, <see cref="Boolean"/>, <see cref="Int32"/>, or <see cref="DateTime"/>, the <see
    ///   cref="SetScalarValue(ITopicBindingModel, Topic, PropertyConfiguration)"/> method will attempt to set the property on
    ///   the <paramref name="target"/>. If the value is not set on the <paramref name="source"/> then the <see
    ///   cref="DefaultValueAttribute"/> will be evaluated as a fallback. If the property is not of a settable type then the property
    ///   is not set. If the value is empty, then it will be treated as <c>null</c> in the <paramref name="target"/>'s <see
    ///   cref="AttributeValueCollection"/>.
    /// </remarks>
    /// <param name="source">
    ///   The binding model from which to derive the data. Must inherit from <see cref="ITopicBindingModel"/>.
    /// </param>
    /// <param name="target">The <see cref="Topic"/> entity to map the data to.</param>
    /// <param name="configuration">The <see cref="PropertyConfiguration"/> with details about the property's attributes.</param>
    /// <autogeneratedoc />
    protected static void SetScalarValue(ITopicBindingModel source, Topic target, PropertyConfiguration configuration) {

      /*------------------------------------------------------------------------------------------------------------------------
      | Escape clause if preconditions are not met
      \-----------------------------------------------------------------------------------------------------------------------*/
      //#### TODO JC20190219: Validate using the ITopicRepository's ContentTypeDescriptor to ensure it's a valid attribute

      /*------------------------------------------------------------------------------------------------------------------------
      | Attempt to retrieve value from the binding model property
      \-----------------------------------------------------------------------------------------------------------------------*/
      var attributeValue = _typeCache.GetPropertyValue(source, configuration.Property.Name)?.ToString();

      /*------------------------------------------------------------------------------------------------------------------------
      | Fall back to default, if configured
      \-----------------------------------------------------------------------------------------------------------------------*/
      if (String.IsNullOrEmpty(attributeValue)) {
        attributeValue = configuration.DefaultValue.ToString();
      }

      /*------------------------------------------------------------------------------------------------------------------------
      | Set the value (to null, if appropriate)
      \-----------------------------------------------------------------------------------------------------------------------*/
      target.Attributes.SetValue(configuration.AttributeKey, attributeValue);

    }

    /*==========================================================================================================================
    | PROTECTED: SET COLLECTION VALUE
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Given a collection property, identifies a target collection, maps the values from the binding model to target <see
    ///   cref="Topic"/>s, and attempts to add them to the target collection.
    /// </summary>
    /// <remarks>
    ///   Given a source collection <paramref name="configuration"/> on a <paramref name="target"/> binding model, attempts to
    ///   identify a source collection on the <paramref name="target"/>. Collections can be mapped to <see
    ///   cref="Topic.Relationships"/>, <see cref="Topic.IncomingRelationships"/> or to a nested target (which is part of
    ///   <see cref="Topic.Children"/>). By default, <see cref="ReverseTopicMappingService"/> will attempt to map based on the
    ///   property name, though this behavior can be modified using the <paramref name="configuration"/>, based on annotations
    ///   on the <paramref name="source"/> binding model. The <see cref="ReverseTopicMappingService"/> does <i>not</i> support
    ///   mapping <see cref="Topic.Children"/>.
    /// </remarks>
    /// <param name="source">
    ///   The binding model from which to derive the data. Must inherit from <see cref="ITopicBindingModel"/>.
    /// </param>
    /// <param name="target">The <see cref="Topic"/> entity to map the data to.</param>
    /// <param name="configuration">
    ///   The <see cref="PropertyConfiguration"/> with details about the property's attributes.
    /// </param>
    protected async Task SetCollectionValueAsync(
      ITopicBindingModel        source,
      Topic                     target,
      PropertyConfiguration     configuration
    ) {

      /*------------------------------------------------------------------------------------------------------------------------
      | Escape clause if preconditions are not met
      \-----------------------------------------------------------------------------------------------------------------------*/
      //#### TODO JJC20190221: If this is a relationship collection, then the list will be IRelatedTopicBindingModel. This needs
      //to address that scenario. This may require splitting this up into two methods (one for relationships, another for
      //children). That may make sense regardless since the way they are set will also vary.
      if (!typeof(IList<ITopicBindingModel>).IsAssignableFrom(configuration.Property.PropertyType)) return;

      /*------------------------------------------------------------------------------------------------------------------------
      | Ensure source list is created
      \-----------------------------------------------------------------------------------------------------------------------*/
      var sourceList = (IList<ITopicBindingModel>)configuration.Property.GetValue(source, null)?? new List<ITopicBindingModel>();

      /*------------------------------------------------------------------------------------------------------------------------
      | Establish target collection to store topics to be mapped
      \-----------------------------------------------------------------------------------------------------------------------*/
      //#### TODO JC20190219: This is now just an IList and can be retrieved using Configuration.Property.GetValue()
      //#### TODO JC20190219: May need to create a child List ContentType that IsHidden for Nested Topics
      var targetList = GetTargetCollection(target, configuration);

      /*------------------------------------------------------------------------------------------------------------------------
      | Validate that target collection was identified
      \-----------------------------------------------------------------------------------------------------------------------*/
      if (targetList == null) return;

      /*------------------------------------------------------------------------------------------------------------------------
      | Map the topics from the source collection, and add them to the target collection
      \-----------------------------------------------------------------------------------------------------------------------*/
      //#### TODO JC20190219: If it's a relationships collection (and that's all we should be supporting right now!) then it
      //should be using target.SetRelationship().
      await PopulateTargetCollectionAsync(sourceList, targetList, configuration).ConfigureAwait(false);

    }

    /*==========================================================================================================================
    | PROTECTED: GET TARGET COLLECTION
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Given a target <see cref="Topic"/> and a <see cref="PropertyConfiguration"/>, attempts to identify the target
    ///   collection that is associated with the source property on the binding model.
    /// </summary>
    /// <remarks>
    ///   Collections can be mapped to <see cref="Topic.Relationships"/>, <see cref="Topic.IncomingRelationships"/> or to a
    ///   nested target (which will be part of <see cref="Topic.Children"/>). By default, <see
    ///   cref="ReverseTopicMappingService"/> will attempt to map based on the property name, though this behavior can be
    ///   modified using the <paramref name="configuration"/>, based on annotations on the source binding model.
    /// </remarks>
    /// <param name="target">
    ///   The binding model from which to derive the data. Must inherit from <see cref="ITopicBindingModel"/>.
    /// </param>
    /// <param name="configuration">
    ///   The <see cref="PropertyConfiguration"/> with details about the property's attributes.
    /// </param>
    /// <exception cref="InvalidOperationException">
    ///   <see cref="ReverseTopicMappingService"/> does not support mapping to <see cref="Topic.Children"/>. Any attempts to do
    ///   so will raise an <see cref="InvalidOperationException"/>. If mapping of children is necessary, then it should be
    ///   handled by the calling class on a per child basis. The caller will have better awareness of the context, and is thus
    ///   better suited to validate the request and handle potential issues with merging—such as deleting entire <see
    ///   cref="Topic"/> trees.
    /// </exception>
    protected IList<Topic> GetTargetCollection(Topic target, PropertyConfiguration configuration) {

      /*------------------------------------------------------------------------------------------------------------------------
      | Establish source collection to store topics to be mapped
      \-----------------------------------------------------------------------------------------------------------------------*/
      var                       listSource                      = (IList<Topic>)Array.Empty<Topic>();
      var                       relationshipKey                 = configuration.RelationshipKey;
      var                       relationshipType                = configuration.RelationshipType;

      /*------------------------------------------------------------------------------------------------------------------------
      | Handle (outgoing) relationships
      \-----------------------------------------------------------------------------------------------------------------------*/
      if (relationshipType.Equals(RelationshipType.Relationship) && target.Relationships.Contains(relationshipKey)) {
        listSource = target.Relationships.GetTopics(relationshipKey);
      }

      /*------------------------------------------------------------------------------------------------------------------------
      | Handle nested topics, or children corresponding to the property name
      \-----------------------------------------------------------------------------------------------------------------------*/
      //#### TODO JJC20190221: Should create the interstitial "List" container if it's missing
      if (relationshipType.Equals(RelationshipType.NestedTopics) && target.Children.Contains(relationshipKey)) {
        listSource = target.Children[relationshipKey].Children;
      }

      /*------------------------------------------------------------------------------------------------------------------------
      | Handle (incoming) relationships
      \-----------------------------------------------------------------------------------------------------------------------*/
      if (relationshipType.Equals(RelationshipType.IncomingRelationship) && target.IncomingRelationships.Contains(relationshipKey)) {
        listSource = target.IncomingRelationships.GetTopics(relationshipKey);
      }

      /*------------------------------------------------------------------------------------------------------------------------
      | Handle Metadata relationship
      \-----------------------------------------------------------------------------------------------------------------------*/
      if (listSource.Count == 0 && !String.IsNullOrWhiteSpace(configuration.MetadataKey)) {
        var metadataKey = $"Root:Configuration:Metadata:{configuration.MetadataKey}:LookupList";
        listSource = _topicRepository.Load(metadataKey)?.Children.ToList();
      }

      return listSource;

    }

    /*==========================================================================================================================
    | PROTECTED: POPULATE TARGET COLLECTION
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Given a source list, will populate a target list based on the configured behavior of the source property.
    /// </summary>
    /// <param name="sourceList">The <see cref="IList{ITopicBindingModel}"/> to pull the binding models from.</param>
    /// <param name="targetList">The target <see cref="IList{Topic}"/> to add the mapped <see cref="Topic"/> objects to.</param>
    /// <param name="configuration">
    ///   The <see cref="PropertyConfiguration"/> with details about the property's attributes.
    /// </param>
    protected async Task PopulateTargetCollectionAsync(
      IList<ITopicBindingModel> sourceList,
      IList<Topic>              targetList,
      PropertyConfiguration     configuration
    ) {

      /*------------------------------------------------------------------------------------------------------------------------
      | Queue up mapping tasks
      \-----------------------------------------------------------------------------------------------------------------------*/
      var taskQueue = new List<Task<Topic>>();

      //Map child binding model to target collection on the target
      foreach (var childBindingModel in sourceList) {
        taskQueue.Add(MapAsync(childBindingModel));
      }

      /*------------------------------------------------------------------------------------------------------------------------
      | Process mapping tasks
      \-----------------------------------------------------------------------------------------------------------------------*/
      while (taskQueue.Count > 0) {
        var topicTask = await Task.WhenAny(taskQueue).ConfigureAwait(false);
        taskQueue.Remove(topicTask);
        targetList.Add(await topicTask.ConfigureAwait(false));
      }

    }

  } //Class
} //Namespace