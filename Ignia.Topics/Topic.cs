/*==============================================================================================================================
| Author        Ignia, LLC
| Client        Ignia, LLC
| Project       Topics Library
\=============================================================================================================================*/
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Globalization;
using System.Diagnostics.Contracts;
using System.Text.RegularExpressions;
using Ignia.Topics.Collections;
using Ignia.Topics.Repositories;

namespace Ignia.Topics {

  /*============================================================================================================================
  | CLASS: TOPIC
  \---------------------------------------------------------------------------------------------------------------------------*/
  /// <summary>
  ///   The Topic object is a simple container for a particular node in the topic hierarchy. It contains the metadata associated
  ///   with the particular node, a list of children, etc.
  /// </summary>
  public class Topic : IDisposable {

    /*==========================================================================================================================
    | PRIVATE VARIABLES
    \-------------------------------------------------------------------------------------------------------------------------*/
    private                     int                             _id                             = -1;
    private                     string                          _key                            = null;
    private                     string                          _originalKey                    = null;
    private                     Topic                           _parent                         = null;
    private                     TopicCollection                 _children                       = null;
    private                     AttributeValueCollection        _attributes                     = null;
    private                     RelatedTopicCollection          _relationships                  = null;
    private                     RelatedTopicCollection          _incomingRelationships          = null;
    private                     Topic                           _derivedTopic                   = null;
    private                     List<DateTime>                  _versionHistory                 = null;

    /*==========================================================================================================================
    | CONSTRUCTOR
    >-----------=---------------------------------------------------------------------------------------------------------------
    | ### NOTE JJC082715: The empty constructor is a prerequisite of the factory method, which relies on Activator to create a
    | new instance of the object.
    \-----------=-------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Initializes a new instance of the <see cref="Topic"/> class.
    /// </summary>
    public Topic() { }

    #region Core Properties

    /*==========================================================================================================================
    | PROPERTY: ID
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Gets or sets the topic's integer identifier according to the data provider.
    /// </summary>
    /// <requires description="The id is expected to be a positive value." exception="T:System.ArgumentException">
    ///   value > 0
    /// </requires>
    public int Id {
      get => _id;
      set {
        Contract.Requires<ArgumentOutOfRangeException>(value > 0, "The id is expected to be a positive value.");
        if (_id > 0 && !_id.Equals(value)) {
          throw new ArgumentException("The value of this topic has already been set to " + _id + "; it cannot be changed.");
        }
        _id = value;
      }
    }

    /*==========================================================================================================================
    | PROPERTY: PARENT
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Reference to the parent topic of this node, allowing code to traverse topics as a linked list.
    /// </summary>
    /// <remarks>
    ///   While topics may be represented as a network graph via relationships, they are physically stored and primarily
    ///   represented via a hierarchy. As such, each topic may have at most a single parent. Note that the the root node will
    ///   have a null parent.
    /// </remarks>
    /// <requires description="The value for Parent must not be null." exception="T:System.ArgumentNullException">
    ///   value != null
    /// </requires>
    /// <requires description="A topic cannot be its own parent." exception="T:System.ArgumentException">
    ///   value != this
    /// </requires>
    public Topic Parent {
      get => _parent;
      set {
        if (_parent != value) {
          SetParent(value, value?.Children?.LastOrDefault());
        }
      }
    }

    /*==========================================================================================================================
    | PROPERTY: CHILDREN
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Provides a keyed collection of child <see cref="Topic"/> instances associated with the current <see cref="Topic"/>.
    /// </summary>
    public TopicCollection Children {
      get {
        Contract.Ensures(Contract.Result<TopicCollection>() != null);
        if (_children == null) {
          _children = new TopicCollection(this);
        }
        return _children;
      }
    }

    /*==========================================================================================================================
    | PROPERTY: IS EMPTY
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Gets whether the Topic's Key is invalid (null or empty).
    /// </summary>
    public bool IsEmpty => String.IsNullOrEmpty(Key);

    /*==========================================================================================================================
    | PROPERTY: CONTENT TYPE
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Gets the key name of the content type that the current topic represents.
    /// </summary>
    /// <remarks>
    ///   Each topic is associated with a content type. The content type determines which attributes are displayed in the Topics
    ///   Editor (via the <see cref="ContentTypeDescriptor.AttributeDescriptors"/> property). The content type also determines,
    ///   by default, which view is rendered by the <see cref="Topics.ITopicRoutingService"/> (assuming the value isn't
    ///   overwritten down the pipe).
    /// </remarks>
    public string ContentType {
      get => Attributes.GetValue("ContentType");
      set => SetAttributeValue("ContentType", value);
    }

    /*==========================================================================================================================
    | PROPERTY: KEY
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Gets or sets the topic's Key attribute, the primary text identifier for the topic.
    /// </summary>
    /// <requires description="The value from the getter must not be null." exception="T:System.ArgumentNullException">
    ///   value != null
    /// </requires>
    /// <requires
    ///   description="The Key should be an alphanumeric sequence; it should not contain spaces or symbols."
    ///   exception="T:System.ArgumentException">
    ///   !value.Contains(" ")
    /// </requires>
    [AttributeSetter]
    public string Key {
      get => _key;
      set {
        TopicFactory.ValidateKey(value);
        if (_originalKey == null) {
          _originalKey = Attributes.GetValue("Key", _key, false, false);
        }
        //If an established key value is changed, the parent's index must be manually updated; this won't happen automatically.
        if (_originalKey != null && !value.Equals(_key) && Parent != null) {
          Parent.Children.ChangeKey(this, value);
        }
        SetAttributeValue("Key", value);
        _key = value;
      }
    }

    /*==========================================================================================================================
    | PROPERTY: ORIGINAL KEY
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Gets or sets the topic's original key.
    /// </summary>
    /// <remarks>
    ///   The original key is automatically set by <see cref="Key"/> when its value is updated (assuming the original key isn't
    ///   already set). This is, in turn, used by the <see cref="Repositories.RenameEventArgs"/> to represent the original value,
    ///   and thus allow the <see cref="Repositories.ITopicRepository"/> (or derived providers) from updating the data store
    ///   appropriately.
    /// </remarks>
    /// <requires
    ///   description="The OriginalKey should be an alphanumeric sequence; it should not contain spaces or symbols."
    ///   exception="T:System.ArgumentException">
    ///   !value?.Contains(" ")?? true
    /// </requires>
    internal string OriginalKey {
      get => _originalKey;
      set {
        TopicFactory.ValidateKey(value, true);
        _originalKey = value;
      }
    }

    #endregion

    #region Convenience Properties

    /*==========================================================================================================================
    | PROPERTY: VIEW
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Gets or sets the View attribute, representing the default view to be used for the topic.
    /// </summary>
    /// <remarks>
    ///   This value can be set via the query string (via the <see cref="ITopicRoutingService"/> class), via the Accepts header
    ///   (also via the <see cref="ITopicRoutingService"/> class), on the topic itself (via this property). By default, it will
    ///   be set to the name of the <see cref="ContentType"/>; e.g., if the Content Type is "Page", then the view will be
    ///   "Page". This will cause the <see cref="ITopicRoutingService"/> to look for a view at, for instance,
    ///   /Common/Templates/Page/Page.aspx.
    /// </remarks>
    /// <requires
    ///   description="The View should be an alphanumeric sequence; it should not contain spaces or symbols."
    ///   exception="T:System.ArgumentException">
    ///   !value?.Contains(" ")?? true
    /// </requires>
    [AttributeSetter]
    public string View {
      get =>
        Attributes.GetValue("View", "");
      set {
        TopicFactory.ValidateKey(value, true);
        SetAttributeValue("View", value);
      }
    }

    /*==========================================================================================================================
    | PROPERTY: IS HIDDEN
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Gets or sets whether the current topic is hidden.
    /// </summary>
    [AttributeSetter]
    public bool IsHidden {
      get => Attributes.GetValue("IsHidden", "0").Equals("1");
      set => SetAttributeValue("IsHidden", value ? "1" : "0");
    }

    /*==========================================================================================================================
    | PROPERTY: IS DISABLED
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Gets or sets whether the current topic is disabled.
    /// </summary>
    [AttributeSetter]
    public bool IsDisabled {
      get => Attributes.GetValue("IsDisabled", "0").Equals("1");
      set => SetAttributeValue("IsDisabled", value ? "1" : "0");
    }

    /*==========================================================================================================================
    | METHOD: IS VISIBLE
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Determines whether or not a topic should be visible based on IsHidden, IsDisabled, and an optional parameter
    ///   specifying whether or not to show disabled items (which may by triggered if, for example, a user is an administrator).
    /// </summary>
    /// <remarks>
    ///   If an item is not marked as IsVisible, then the item will not be visible independent of whether showDisabled is set.
    /// </remarks>
    /// <param name="showDisabled">Determines whether or not items marked as IsDisabled should be displayed.</param>
    public bool IsVisible(bool showDisabled = false) => !IsHidden && (showDisabled || !IsDisabled);

    /*==========================================================================================================================
    | PROPERTY: TITLE
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Gets or sets the Title attribute, which represents the friendly name of the topic.
    /// </summary>
    /// <remarks>
    ///   While the <see cref="Key"/> may not contain, for instance, spaces or symbols, there are no restrictions on what
    ///   characters can be used in the title. For this reason, it provides the default public value for referencing topics. If
    ///   the title is not set, then this property falls back to the topic's <see cref="Key"/>.
    /// </remarks>
    /// <requires description="The value from the getter must be provided." exception="T:System.ArgumentNullException">
    ///   !string.IsNullOrWhiteSpace(value)
    /// </requires>
    public string Title {
      get => Attributes.GetValue("Title", Key);
      set => SetAttributeValue("Title", value);
    }

    /*==========================================================================================================================
    | PROPERTY: DESCRIPTION
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Gets or sets the Description attribute.
    /// </summary>
    /// <remarks>
    ///   The Description attribute is primarily used by the editor to display help content for an attribute topic, noting
    ///   how the attribute is used, what is the expected input format or value, etc.
    /// </remarks>
    /// <requires description="The value from the getter must be provided." exception="T:System.ArgumentNullException">
    ///   !string.IsNullOrWhiteSpace(value)
    /// </requires>
    public string Description {
      get => Attributes.GetValue("Description");
      set => SetAttributeValue("Description", value);
    }

    /*==========================================================================================================================
    | PROPERTY: LAST MODIFIED
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Gets or sets the topic's last modified attribute.
    /// </summary>
    /// <remarks>
    ///   The value is stored in the database as a string (Attribute) value, but converted to DateTime for use in the system. It
    ///   is important to note that the last modified attribute is not tied to the system versioning (which operates at an
    ///   attribute level) nor is it guaranteed to be correct for auditing purposes; for example, the author may explicitly
    ///   overwrite this value for various reasons (such as backdating a webpage).
    /// </remarks>
    /// <requires description="The value from the getter must be provided." exception="T:System.ArgumentNullException">
    ///   !string.IsNullOrWhiteSpace(value.ToString())
    /// </requires>
    public DateTime LastModified {
      get {

        /*----------------------------------------------------------------------------------------------------------------------
        | Establish default value
        \---------------------------------------------------------------------------------------------------------------------*/
        var defaultValue = VersionHistory.Count > 0 ? VersionHistory.LastOrDefault() : DateTime.MinValue;

        /*----------------------------------------------------------------------------------------------------------------------
        | Return converted string attribute value, if available
        \---------------------------------------------------------------------------------------------------------------------*/
        var lastModified = Attributes.GetValue("LastModified", defaultValue.ToString());

        // Return converted DateTime
        if (DateTime.TryParse(lastModified, out var dateTimeValue)) {
          return dateTimeValue;
        }

        /*----------------------------------------------------------------------------------------------------------------------
        | Otherwise, return default of minimum value
        \---------------------------------------------------------------------------------------------------------------------*/
        return defaultValue;

      }
      set => SetAttributeValue("LastModified", value.ToString());
    }

    #endregion

    #region Relationship and Collection Methods

    /*==========================================================================================================================
    | METHOD: SET PARENT
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Changes the current <see cref="Parent"/> while simultenaously ensuring that the sort order of the topics is
    ///   maintained, assuming a <paramref name="sibling"/> is set.
    /// </summary>
    /// <remarks>
    ///   If no <paramref name="sibling"/> is provided, then the item is added to the <i>beginning</i> of the collection. If
    ///   the intent is to add it to the <i>end</i> of the collection, then set the <paramref name="sibling"/> to e.g.
    ///   <c>parent.Children.LastOrDefault()</c>.
    /// </remarks>
    /// <param name="parent">The <see cref="Topic"/> to move this <see cref="Topic"/> under.</param>
    /// <param name="sibling">The <see cref="Topic"/> to mvoe this <see cref="Topic"/> to the right of.</param>
    public void SetParent(Topic parent, Topic sibling = null) {

      /*------------------------------------------------------------------------------------------------------------------------
      | Check preconditions
      \-----------------------------------------------------------------------------------------------------------------------*/
      Contract.Requires<ArgumentNullException>(parent != null, "The value for Parent must not be null.");
      Contract.Requires<ArgumentOutOfRangeException>(parent != this, "A topic cannot be its own parent.");

      /*------------------------------------------------------------------------------------------------------------------------
      | Check to ensure that the topic isn't being moved to a descendant (topics cannot be their own grandpa)
      \-----------------------------------------------------------------------------------------------------------------------*/
      if (parent.GetUniqueKey().StartsWith(GetUniqueKey())) {
        throw new ArgumentOutOfRangeException(nameof(parent), "A descendant cannot be its own parent.");
      }

      /*------------------------------------------------------------------------------------------------------------------------
      | Check to ensure that the topic isn't being moved to a parent with a duplicate key
      \-----------------------------------------------------------------------------------------------------------------------*/
      if (parent != _parent && parent.Children.Contains(Key)) {
        throw new InvalidKeyException(
          "Duplicate key when setting Parent property: the topic with the name '" + Key +
          "' already exists in the '" + parent.Key + "' topic."
          );
      }

      /*------------------------------------------------------------------------------------------------------------------------
      | Move topic to new location
      \-----------------------------------------------------------------------------------------------------------------------*/
      if (_parent != null) {
        _parent.Children.Remove(Key);
      }
      var insertAt = (sibling != null)? parent.Children.IndexOf(sibling)+1 : 0;
      parent.Children.Insert(insertAt, this);

      /*------------------------------------------------------------------------------------------------------------------------
      | Set parent values
      \-----------------------------------------------------------------------------------------------------------------------*/
      if (_parent != parent) {
        _parent = parent;
        SetAttributeValue("ParentID", parent.Id.ToString(CultureInfo.InvariantCulture));
      }


    }

    /*==========================================================================================================================
    | METHOD: GET UNIQUE KEY
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Gets the full, hierarchical identifier for the topic, including parents.
    /// </summary>
    /// <remarks>
    ///   The value for the UniqueKey property is a collated, colon-delimited representation of the topic and its parent(s).
    ///   Example: "Root:Configuration:ContentTypes:Page".
    /// </remarks>
    public string GetUniqueKey() {

      /*------------------------------------------------------------------------------------------------------------------------
      | Validate return value
      \-----------------------------------------------------------------------------------------------------------------------*/
      Contract.Ensures(Contract.Result<string>() != null);

      /*------------------------------------------------------------------------------------------------------------------------
      | Crawl up tree to define uniqueKey
      \-----------------------------------------------------------------------------------------------------------------------*/
      var uniqueKey = "";
      var topic = this;

      for (var i = 0; i < 100; i++) {
        if (uniqueKey.Length > 0) uniqueKey = ":" + uniqueKey;
        uniqueKey = topic.Key + uniqueKey;
        topic = topic.Parent;
        if (topic == null) break;
      }

      /*------------------------------------------------------------------------------------------------------------------------
      | Return value
      \-----------------------------------------------------------------------------------------------------------------------*/
      return uniqueKey;

    }

    /*==========================================================================================================================
    | METHOD: GET WEB PATH
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Gets the root-relative web path of the Topic, based on an assumption that the root topic is bound to the root of the
    ///   site.
    /// </summary>
    /// <remarks>
    ///   Note: If the topic root is not bound to the root of the site, this needs to specifically accounted for in any views
    ///   that reference the web path (e.g., by providing a prefix).
    /// </remarks>
    public string GetWebPath() {
      Contract.Ensures(Contract.Result<string>() != null);
      var uniqueKey = GetUniqueKey().Replace("Root:", "/").Replace(":", "/") + "/";
      if (!uniqueKey.StartsWith("/")) {
        uniqueKey = "/" + uniqueKey;
      }
      return uniqueKey;
    }

    #endregion

    #region Relationship and Collection Properties

    /*==========================================================================================================================
    | PROPERTY: DERIVED TOPIC
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Reference to the topic that this topic is derived from, if available.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Derived topics allow attribute values to be inherited from another topic. When a derived topic is configured via the
    ///     TopicId attribute key, values from that topic are used when the <see cref="AttributeValueCollection.GetValue(String,
    ///     Boolean)"/> method unable to find a local value for the attribute.
    ///   </para>
    ///   <para>
    ///     Be aware that while multiple levels of derived topics can be configured, the <see
    ///     cref="AttributeValueCollection.GetValue(String, Boolean)"/> method defaults to a maximum level of five "hops".
    ///   </para>
    /// </remarks>
    /// <requires description="A topic key must not derive from itself." exception="T:System.ArgumentException">
    ///   value != this
    /// </requires>
    public Topic DerivedTopic {
      get => _derivedTopic;
      set {
        Contract.Requires<ArgumentException>(
          value != this,
          "A topic may not derive from itself."
        );
        _derivedTopic = value;
        if (value != null) {
          SetAttributeValue("TopicID", value.Id.ToString());
        }
        else {
          Attributes.Remove("TopicID");
        }
      }
    }

    /*==========================================================================================================================
    | PROPERTY: ATTRIBUTES
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Attributes is a generic property bag for keeping track of either named or arbitrary attributes, thus providing
    ///   significant extensibility.
    /// </summary>
    /// <remarks>
    ///   Attributes are stored via an <see cref="AttributeValue"/> class which, in addition to the Attribute Key and Value,
    ///   also track other metadata for the attribute, such as the version (via the <see cref="AttributeValue.LastModified"/>
    ///   property) and whether it has been persisted to the database or not (via the <see cref="AttributeValue.IsDirty"/>
    ///   property).
    /// </remarks>
    public AttributeValueCollection Attributes {
      get {
        Contract.Ensures(Contract.Result<AttributeValueCollection>() != null);
        if (_attributes == null) {
          _attributes = new AttributeValueCollection(this);
        }
        return _attributes;
      }

    /*==========================================================================================================================
    | PROPERTY: RELATIONSHIPS
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   A fa�ade for accessing related topics based on a scope name; can be used for tags, related topics, etc.
    /// </summary>
    /// <remarks>
    ///   The relationships property exposes a <see cref="Topic"/> with child topics representing named relationships (e.g.,
    ///   "Related" for related topics); those child topics in turn have child topics representing references to each related
    ///   topic, thus allowing the topic hierarchy to be represented as a network graph.
    /// </remarks>
    public RelatedTopicCollection Relationships {
      get {
        Contract.Ensures(Contract.Result<RelatedTopicCollection>() != null);
        if (_relationships == null) {
          _relationships = new RelatedTopicCollection(this, false);
        }
        return _relationships;
      }
    }

    /*===========================================================================================================================
    | PROPERTY: INCOMING RELATIONSHIPS
    \--------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   A fa�ade for accessing related topics based on a scope name; can be used for tags, related topics, etc.
    /// </summary>
    /// <remarks>
    ///   The incoming relationships property provides a reverse index of the <see cref="Relationships"/> property, in order to
    ///   indicate which topics point to the current topic. This can be useful for traversing the topic tree as a network graph.
    ///   This is of particular use for tags, where the current topic represents a tag, and the incoming relationships represents
    ///   all topics associated with that tag.
    /// </remarks>
    public RelatedTopicCollection IncomingRelationships {
      get {
        Contract.Ensures(Contract.Result<RelatedTopicCollection>() != null);
        if (_incomingRelationships == null) {
          _incomingRelationships = new RelatedTopicCollection(this, true);
        }
        return _incomingRelationships;
      }
    }

    /*==========================================================================================================================
    | PROPERTY: VERSION HISTORY
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Provides a collection of dates representing past versions of the topic, which can be rolled back to.
    /// </summary>
    /// <remarks>
    ///   It is expected that this collection will be populated by the <see cref="Repositories.ITopicRepository"/> (or one of
    ///   its derived providers).
    /// </remarks>
    public List<DateTime> VersionHistory {
      get {
        Contract.Ensures(Contract.Result<List<DateTime>>() != null);
        if (_versionHistory == null) {
          _versionHistory = new List<DateTime>();
        }
        return _versionHistory;
      }
    }

    #endregion

    #region Collection Methods

    /*==========================================================================================================================
    | METHOD: SET ATTRIBUTE VALUE
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Protected helper method that either adds a new <see cref="AttributeValue"/> object or updates the value of an existing
    ///   one, depending on whether that value already exists.
    /// </summary>
    /// <remarks>
    ///   When an attribute value is set and a corresponding, writable property exists on the topic, that property will be
    ///   called by the AttributeValueCollection.This is intended to enforce local business logic, and prevent callers from
    ///   introducing invalid data.To prevent a redirect loop, however, local properties need to inform the
    ///   AttributeValueCollection that the business logic has already been enforced.To do that, they must either call
    ///   SetValue() with the enforceBusinessLogic flag set to false, or, if they're in a separate assembly, call this overload.
    /// </remarks>
    /// <param name="key">The string identifier for the AttributeValue.</param>
    /// <param name="value">The text value for the AttributeValue.</param>
    /// <param name="isDirty">
    ///   Specified whether the value should be marked as <see cref="AttributeValue.IsDirty"/>. By default, it will be marked as
    ///   dirty if the value is new or has changed from a previous value. By setting this parameter, that behavior is
    ///   overwritten to accept whatever value is submitted. This can be used, for instance, to prevent an update from being
    ///   persisted to the data store on <see cref="ITopicRepository.Save(Topic, Boolean, Boolean)"/>.
    /// </param>
    /// <requires
    ///   description="The key must be specified for the AttributeValue key/value pair."
    ///   exception="T:System.ArgumentNullException">
    ///   !String.IsNullOrWhiteSpace(key)
    /// </requires>
    /// <requires
    ///   description="The value must be specified for the AttributeValue key/value pair."
    ///   exception="T:System.ArgumentNullException">
    ///   !String.IsNullOrWhiteSpace(value)
    /// </requires>
    /// <requires
    ///   description="The key should be an alphanumeric sequence; it should not contain spaces or symbols"
    ///   exception="T:System.ArgumentException">
    ///   !value.Contains(" ")
    /// </requires>
    protected void SetAttributeValue(string key, string value, bool? isDirty = null) {
      Contract.Requires(!String.IsNullOrWhiteSpace(key));
      Attributes.SetValue(key, value, isDirty, false);
    }

    #endregion

    #region Interface Implementations

    /*==========================================================================================================================
    | METHOD: DISPOSE
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Technically, there's nothing to be done when disposing a Topic. However, this allows the topic attributes (and
    ///   properties) to be set using a using statement, which is syntactically convenient.
    /// </summary>
    [Obsolete("There is no need to dispose of the Topic class, and reliance on this should be removed.", false)]
    public void Dispose() {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    /// <summary>
    ///   Protected implementation of <see cref="Dispose(Boolean)"/> for derived types.
    /// </summary>
    [Obsolete("There is no need to dispose of the Topic class, and reliance on this should be removed.", false)]
    protected virtual void Dispose(bool disposing) {
      return;
    }

  #endregion

  } // Class

} // Namespace
