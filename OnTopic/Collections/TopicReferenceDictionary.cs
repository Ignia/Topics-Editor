﻿/*==============================================================================================================================
| Author        Ignia, LLC
| Client        Ignia, LLC
| Project       Topics Library
\=============================================================================================================================*/
using System;
using System.Collections;
using System.Collections.Generic;
using OnTopic.Internal.Diagnostics;

namespace OnTopic.Collections {

  /*============================================================================================================================
  | CLASS: TOPIC REFERENCE DICTIONARY
  \---------------------------------------------------------------------------------------------------------------------------*/
  /// <summary>
  ///   Represents a collection of <see cref="Topic"/> objects associated with particular reference keys.
  /// </summary>
  public class TopicReferenceDictionary : IDictionary<string, Topic> {

    /*==========================================================================================================================
    | PRIVATE VARIABLES
    \-------------------------------------------------------------------------------------------------------------------------*/
    readonly                    Topic                           _parent;
    readonly                    IDictionary<string, Topic>      _storage;

    /*==========================================================================================================================
    | CONSTRUCTOR
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Initializes a new instance of the <see cref="TopicReferenceDictionary"/>.
    /// </summary>
    public TopicReferenceDictionary(Topic parent) {

      /*------------------------------------------------------------------------------------------------------------------------
      | Validate parameters
      \-----------------------------------------------------------------------------------------------------------------------*/
      Contract.Requires(parent, nameof(parent));

      /*------------------------------------------------------------------------------------------------------------------------
      | Initialize backing fields
      \-----------------------------------------------------------------------------------------------------------------------*/
      _parent = parent;
      _storage = new Dictionary<string, Topic>();

    }

    /*==========================================================================================================================
    | COUNT
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <inheritdoc/>
    public int Count => _storage.Count;

    /*==========================================================================================================================
    | IsReadOnly
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <inheritdoc/>
    public bool IsReadOnly => false;

    /*==========================================================================================================================
    | ITEM
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <inheritdoc/>
    public Topic this[string referenceKey] {
      get => _storage[referenceKey];
      set {
        Contract.Requires<ArgumentException>(
          value != _parent,
          "A topic reference may not point to itself."
        );
        if (!_storage.TryGetValue(referenceKey, out var existing) || existing != value) {
          IsDirty = true;
        }
        _storage[referenceKey] = value;
      }
    }

    /*==========================================================================================================================
    | KEYS
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <inheritdoc/>
    public ICollection<string> Keys => _storage.Keys;

    /*==========================================================================================================================
    | VALUES
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <inheritdoc/>
    public ICollection<Topic> Values => _storage.Values;

    /*==========================================================================================================================
    | ADD
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <inheritdoc/>
    void ICollection<KeyValuePair<string, Topic>>.Add(KeyValuePair<string, Topic> item) {

      /*------------------------------------------------------------------------------------------------------------------------
      | Validate parameters
      \-----------------------------------------------------------------------------------------------------------------------*/
      Contract.Requires(item, nameof(item));

      TopicFactory.ValidateKey(item.Key);

      Contract.Requires<ArgumentException>(
        item.Value != _parent,
        "A topic reference may not point to itself."
      );

      /*------------------------------------------------------------------------------------------------------------------------
      | Mark dirty
      \-----------------------------------------------------------------------------------------------------------------------*/
      if (!_storage.TryGetValue(item.Key, out var existing) || existing != item.Value) {
        IsDirty = true;
      }

      /*------------------------------------------------------------------------------------------------------------------------
      | Handle recipricol references
      \-----------------------------------------------------------------------------------------------------------------------*/
      item.Value.IncomingRelationships.SetTopic(item.Key, item.Value);

      /*------------------------------------------------------------------------------------------------------------------------
      | Add item
      \-----------------------------------------------------------------------------------------------------------------------*/
      _storage.Add(item);

    }

    /// <inheritdoc/>
    public void Add(string key, Topic value) {

      /*------------------------------------------------------------------------------------------------------------------------
      | Validate parameters
      \-----------------------------------------------------------------------------------------------------------------------*/
      Contract.Requires(key, nameof(key));
      Contract.Requires(value, nameof(value));

      /*------------------------------------------------------------------------------------------------------------------------
      | Add item
      \-----------------------------------------------------------------------------------------------------------------------*/
      var self = this as ICollection<KeyValuePair<string, Topic>>;
      self.Add(new(key, value));

    }

    /*==========================================================================================================================
    | SET TOPIC
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Adds a new topic reference—or updates one, if it already exists. If the value is <c>null</c>, and a value exits, it is
    ///   removed.
    /// </summary>
    public void SetTopic(string key, Topic? value, bool? isDirty = null) {
      var wasDirty = IsDirty;
      if (value is null) {
        if (ContainsKey("key")) {
          Remove(key);
        }
      }
      else {
        this[key] = value;
      }
      if (wasDirty is false && isDirty is false) {
        IsDirty = false;
      }
    }

    /*==========================================================================================================================
    | CLEAR
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <inheritdoc/>
    public void Clear() {

      /*------------------------------------------------------------------------------------------------------------------------
      | Mark dirty
      \-----------------------------------------------------------------------------------------------------------------------*/
      if (Count > 0) {
        IsDirty = true;
      }

      /*------------------------------------------------------------------------------------------------------------------------
      | Handle recipricol references
      \-----------------------------------------------------------------------------------------------------------------------*/
      foreach (var item in _storage) {
        item.Value.IncomingRelationships.RemoveTopic(item.Key, _parent);
      }

      /*------------------------------------------------------------------------------------------------------------------------
      | Call base method
      \-----------------------------------------------------------------------------------------------------------------------*/
      _storage.Clear();

    }

    /*==========================================================================================================================
    | CONTAINS
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <inheritdoc/>
    public bool Contains(KeyValuePair<string, Topic> item) => _storage.Contains(item);

    /*==========================================================================================================================
    | CONTAINS KEY
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <inheritdoc/>
    public bool ContainsKey(string key) => _storage.ContainsKey(key);

    /*==========================================================================================================================
    | COPY TO
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <inheritdoc/>
    public void CopyTo(KeyValuePair<string, Topic>[] array, int arrayIndex) => _storage.CopyTo(array, arrayIndex);

    /*==========================================================================================================================
    | GET ENUMERATOR
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => _storage.GetEnumerator();

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<string, Topic>> GetEnumerator() => _storage.GetEnumerator();

    /*==========================================================================================================================
    | REMOVE
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <inheritdoc/>
    bool ICollection<KeyValuePair<string, Topic>>.Remove(KeyValuePair<string, Topic> item) =>
      Contains(item) && Remove(item.Key);

    /// <inheritdoc/>
    public bool Remove(string key) {

      /*------------------------------------------------------------------------------------------------------------------------
      | Validate parameters
      \-----------------------------------------------------------------------------------------------------------------------*/
      Contract.Requires(key, nameof(key));

      /*------------------------------------------------------------------------------------------------------------------------
      | Handle existing
      \-----------------------------------------------------------------------------------------------------------------------*/
      if (TryGetValue(key, out var existing)) {
        existing.IncomingRelationships.RemoveTopic(key, _parent);
        IsDirty = true;
      }

      /*------------------------------------------------------------------------------------------------------------------------
      | Call base method
      \-----------------------------------------------------------------------------------------------------------------------*/
      return _storage.Remove(key);

    }

    /*==========================================================================================================================
    | TRY/GET VALUE
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <inheritdoc/>
    public bool TryGetValue(string key, out Topic value) => _storage.TryGetValue(key, out value);

    /*==========================================================================================================================
    | GET TOPIC
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Attempts to retrieve a topic reference based on its <paramref name="key"/>; if it doesn't exist, returns null.
    /// </summary>
    public Topic? GetTopic(string key) => TryGetValue(key, out var existing)? existing : null;

    /*==========================================================================================================================
    | IS DIRTY?
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Determines if the dictionary has been modified. This value is set to <c>true</c> any time a new item is inserted or
    ///   removed from the dictionary.
    /// </summary>
    public bool IsDirty { get; set; }

  } //Class
} //Namespace