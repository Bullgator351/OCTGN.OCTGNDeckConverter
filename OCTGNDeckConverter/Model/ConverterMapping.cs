﻿// -----------------------------------------------------------------------
// <copyright file="ConverterMapping.cs" company="jlkatz">
// Copyright (c) 2013 Justin L Katz. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace OCTGNDeckConverter.Model
{
    /// <summary>
    /// Represents a parsed Card, Quantity, and possibly Set.  It contains a collection of potential
    /// OCTGN Cards that could be a match, which are exposed to the user so they can choose.
    /// </summary>
    public class ConverterMapping : INotifyPropertyChangedBase
    {
        /// <summary>
        /// Initializes a new instance of the ConverterMapping class.
        /// </summary>
        /// <param name="cardName">The parsed name of the Card</param>
        /// <param name="cardSet">The parsed name (or abbreviation) of the Set the converted Card belongs to.  If unavailable, use string.Empty</param>
        /// <param name="quantity">The quantity of this Card that is included in the Deck</param>
        public ConverterMapping(string cardName, string cardSet, int quantity)
        {
            this.CardName = cardName;
            this.CardSet = cardSet;
            this.Quantity = quantity;
        }

        #region Public Properties

        /// <summary>
        /// Gets the parsed name of the Card
        /// </summary>
        public string CardName
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the parsed name (or abbreviation) of the Set the Card belongs to.  If unavailable, it is string.Empty
        /// </summary>
        public string CardSet
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the quantity of this Card that is included in the Deck
        /// </summary>
        public int Quantity
        {
            get;
            private set;
        }

        /// <summary>
        /// Private backing field
        /// </summary>
        private ObservableCollection<ConverterCard> _PotentialOCTGNCards = new ObservableCollection<ConverterCard>();

        /// <summary>
        /// Private backing field
        /// </summary>
        private ReadOnlyObservableCollection<ConverterCard> _PotentialOCTGNCardsReadOnly;
        
        /// <summary>
        /// Gets the collection of potential OCTGN Cards that match the Imported card.  Sorted by MultiverseID.
        /// </summary>
        public ReadOnlyObservableCollection<ConverterCard> PotentialOCTGNCards
        {
            get 
            {
                if (this._PotentialOCTGNCardsReadOnly == null)
                { 
                    this._PotentialOCTGNCardsReadOnly = new ReadOnlyObservableCollection<ConverterCard>(this._PotentialOCTGNCards); 
                }

                return this._PotentialOCTGNCardsReadOnly;
            }
        }

        /// <summary>
        /// Property name constant
        /// </summary>
        internal const string SelectedOCTGNCardPropertyName = "SelectedOCTGNCard";

        /// <summary>
        /// Private backing field
        /// </summary>
        private ConverterCard _SelectedOCTGNCard;
        
        /// <summary>
        /// Gets or sets the currently selected OCTGN Card.  This card will be the one used when generating the OCTGN deck.
        /// </summary>
        public ConverterCard SelectedOCTGNCard
        {
            get { return this._SelectedOCTGNCard; }
            set { this.SetValue(ref this._SelectedOCTGNCard, value, SelectedOCTGNCardPropertyName); }
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Adds the potentialCard to the PotentialOCTGNCards Collection in MultiverseID order.
        /// </summary>
        /// <param name="potentialCard">The ConverterCard to add to the PotentialOCTGNCards Collection</param>
        /// <returns>True if added, false if not</returns>
        public bool AddPotentialOCTGNCard(ConverterCard potentialCard)
        {
            if (potentialCard == null)
            {
                throw new ArgumentNullException();
            }

            if (this._PotentialOCTGNCards.Contains(potentialCard))
            { 
                return false; 
            }

            if (this._PotentialOCTGNCards.Count == 0)
            {
                // Add it since it is the only item
                this._PotentialOCTGNCards.Add(potentialCard);
            }
            else if (this._PotentialOCTGNCards.Last().MultiverseID > potentialCard.MultiverseID)
            {
                // Add it at the end
                this._PotentialOCTGNCards.Add(potentialCard);
            }
            else
            {
                int index = 0;
                for (int i = 0; i < this._PotentialOCTGNCards.Count; i++)
                {
                    if (this._PotentialOCTGNCards[i].MultiverseID > potentialCard.MultiverseID)
                    { 
                        break; 
                    }
                    else
                    { 
                        index = i; 
                    }
                }

                this._PotentialOCTGNCards.Insert(index, potentialCard);
            }

            return true;
        }

        /// <summary>
        /// Sets the SelectedOCTGNCard to the ConverterCard with the highest Multiverse ID
        /// </summary>
        public void AutoSelectPotentialOCTGNCard()
        {
            if (this.PotentialOCTGNCards.Count == 0)
            {
                this.SelectedOCTGNCard = null;
            }
            else
            {
                ConverterCard maxMultiverseIDCard = this.PotentialOCTGNCards.First();
                foreach (ConverterCard cc in this.PotentialOCTGNCards)
                {
                    if (cc.MultiverseID > maxMultiverseIDCard.MultiverseID)
                    {
                        maxMultiverseIDCard = cc;
                    }
                }

                this.SelectedOCTGNCard = maxMultiverseIDCard;
            }
        }

        /// <summary>
        /// Searches through all converterSets for Cards which potentially match this CardName/CardSet.
        /// Each potential card is added as a potential Card.
        /// </summary>
        /// <param name="converterCardDictionary">
        /// A Dictionary of ConverterCards which are potential matches, where the Key is the normalized first name 
        /// of the card and the value is a collection of all corresponding ConverterCards-ConverterSet tuples.  Only sets which are
        /// included in searches should be in this.</param>
        public void PopulateWithPotentialCards(Dictionary<string, List<Tuple<ConverterCard, ConverterSet>>> converterCardDictionary)//(Dictionary<Guid, ConverterSet> converterSets)
        {
            // Some cards have 2+ names, so create an array of all the names in order
            List<string> converterMappingNames =
                (from string mappingName in this.CardName.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
                 select ConverterMapping.NormalizeName(mappingName)).ToList();

            if (converterCardDictionary.ContainsKey(converterMappingNames.First()))
            {
                foreach (Tuple<ConverterCard, ConverterSet> converterCardAndSet in converterCardDictionary[converterMappingNames.First()])
                {
                    ConverterCard converterCard = converterCardAndSet.Item1;

                    List<string> converterCardNames =
                        (from string cardName in converterCard.Name.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
                         select ConverterMapping.NormalizeName(cardName)).ToList();
                    int numIndexToCompare = Math.Min(converterMappingNames.Count, converterCardNames.Count);

                    // Compare all names.  If one card has less names than the other, then just compare indexes where 
                    // both names are present because sometimes another format only includes the first name
                    bool isNameMatch = true;
                    for (int i = 0; i < numIndexToCompare; i++)
                    {
                        if (!converterCardNames[i].Equals(converterMappingNames[i]))
                        {
                            isNameMatch = false;
                            break;
                        }
                    }

                    bool isSetMatch = true;
                    if (isNameMatch && !string.IsNullOrWhiteSpace(this.CardSet))
                    {
                        ConverterSet converterSet = converterCardAndSet.Item2;

                        // LoTR - Pay attention to Set
                        if (converterSet.OctgnSet.GameId == ConvertEngine.Game.LoTR.GameGuidStatic)
                        {
                            string converterCardSet = converterCard.Set;
                            string converterMappingSet = this.CardSet;

                            // Some sources omit 'The Hobbit - ' in the set name, so remove it from the comparison
                            if (converterCardSet.StartsWith("The Hobbit", StringComparison.InvariantCultureIgnoreCase))
                            {
                                converterCardSet = converterCardSet.Substring(13);
                            }
                            if (converterMappingSet.StartsWith("The Hobbit", StringComparison.InvariantCultureIgnoreCase))
                            {
                                converterMappingSet = converterMappingSet.Substring(13);
                            }

                            // Some sources omit 'The ' in the set name, so remove it from the comparison
                            if (converterCardSet.StartsWith("The ", StringComparison.InvariantCultureIgnoreCase))
                            {
                                converterCardSet = converterCardSet.Substring(4);
                            }
                            if (converterMappingSet.StartsWith("The ", StringComparison.InvariantCultureIgnoreCase))
                            {
                                converterMappingSet = converterMappingSet.Substring(4);
                            }

                            // Remove all Diacritics from names for comparison
                            converterCardSet = ConverterMapping.NormalizeName(converterCardSet);
                            converterMappingSet = ConverterMapping.NormalizeName(converterMappingSet);

                            if (!converterCardSet.Equals(converterMappingSet))
                            {
                                isSetMatch = false;
                            }
                        }
                    }

                    if (isNameMatch && isSetMatch)
                    {
                        this.AddPotentialOCTGNCard(converterCard);
                    }
                }
            }
        }

        /// <summary>
        /// Creates a Dictionary who's Keys are the normalized first name of the Card, and who's Values are all of the matching ConverterCard/ConverterSet Tuples.
        /// </summary>
        /// <param name="converterSets">The ConverterSets to look through to build the normalized ConverterCard Dictionary</param>
        /// <returns>a Dictionary who's Keys are the normalized first name of the Card, and who's Values are all of the matching ConverterCard/ConverterSet Tuples.</returns>
        public static Dictionary<string, List<Tuple<ConverterCard, ConverterSet>>> NormalizeConverterCards(IEnumerable<ConverterSet> converterSets)
        {            
            Dictionary<string, List<Tuple<ConverterCard, ConverterSet>>> normalizedConverterCards = new Dictionary<string,List<Tuple<ConverterCard,ConverterSet>>>();

            foreach (ConverterSet converterSet in converterSets)
            {
                if (converterSet.IncludeInSearches)
                {
                    foreach (ConverterCard converterCard in converterSet.ConverterCards)
                    {
                        string normalizedFirstName = ConverterMapping.NormalizeName(converterCard.Name.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries).First());
                        if (!normalizedConverterCards.ContainsKey(normalizedFirstName))
                        {
                            normalizedConverterCards.Add(normalizedFirstName, new List<Tuple<ConverterCard, ConverterSet>>());
                        }

                        normalizedConverterCards[normalizedFirstName].Add(new Tuple<ConverterCard, ConverterSet>(converterCard, converterSet));
                    }
                }
            }
            return normalizedConverterCards;
        }

        /// <summary>
        /// Removes the potentialCard from the PotentialOCTGNCards Collection
        /// </summary>
        /// <param name="potentialCard">The ConverterCard to remove from the PotentialOCTGNCards Collection</param>
        /// <returns>True if removed, false if not</returns>
        public bool RemovePotentialOCTGNCard(ConverterCard potentialCard)
        {
            return this._PotentialOCTGNCards.Remove(potentialCard);
        }

        #endregion Public Methods

        // Names and sets may or may not contain certain characters, like punctuation.  
        // Replace/remove them for comparison purposes
        private static List<dynamic> replacementChars = new List<dynamic>()
        {
            new { Actual = "Æ", Normalized = "Ae" },
            new { Actual = "æ", Normalized = "ae" },

            new { Actual = '’', Normalized = '\'' },
            new { Actual = ":", Normalized = string.Empty },
            new { Actual = "-", Normalized = string.Empty },
            new { Actual = "'", Normalized = string.Empty },
        };

        /// <summary>
        /// Returns a Name that has been normalized.  This means all funny characters,
        /// punctuation, white space, capitalization, has been removed or sanitized.
        /// </summary>
        /// <param name="name">The name of the Card or Set etc to normalize</param>
        /// <returns></returns>
        private static string NormalizeName(string name)
        {
            // Remove extra whitespace
            name = name.Trim();

            // Remove all Diacritics from names for comparison
            name = ConverterMapping.RemoveDiacritics(name);
            
            // Replace all characters specifically designated to aid in comparison
            foreach (dynamic replacementChar in replacementChars)
            {
                name = name.Replace(replacementChar.Actual, replacementChar.Normalized);
            }

            return name.ToLowerInvariant();
        }

        /// <summary>
        /// Returns a string that has been stripped of diacritics
        /// </summary>
        /// <param name="text">The string to remove diacritics from</param>
        /// <returns>A string that has been stripped of diacritics</returns>
        /// <remarks>Courtesy of: http://stackoverflow.com/a/368850 </remarks>
        private static string RemoveDiacritics(string text)
        {
            return string.Concat(text.Normalize(NormalizationForm.FormD)
                .Where(ch => 
                    System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) !=
                    System.Globalization.UnicodeCategory.NonSpacingMark)
                ).Normalize(NormalizationForm.FormC);
        }
    }
}
