using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;

namespace WFInfo.WFInfoUtil
{
    //good states are below 100
    public enum WordMatchValidity
    {
        GoodMatch = 0,
        TooLargeButEnoughCharacters = 1, // more than 3 characters in a box too large is likely going to be good, pass it but mark as potentially bad
        NotSet = 100,
        TooLarge = 101,
        TooFewCharacters = 102,
    }

    public class WordMatch
    {
        public string word;
        public Rectangle bounds;
        public Rectangle paddedBounds;
        public WordMatchValidity validity;


        public WordMatch(Tuple<string, Rectangle> wordResult)
        {
            word = wordResult.Item1;
            Rectangle bounds = wordResult.Item2;
            int VerticalPad = bounds.Height / 2;
            int HorizontalPad = (int)(bounds.Height * Settings.snapItHorizontalNameMargin);
            paddedBounds = new Rectangle(bounds.X - HorizontalPad, bounds.Y - VerticalPad, bounds.Width + HorizontalPad * 2, bounds.Height + VerticalPad * 2);
        }

        public void SetValidity(double screenScaling)
        {
            //Determine whether or not the box is too large, false positives in OCR can scan items (such as neuroptics, chassis or systems) as a character(s).
            if (paddedBounds.Height > 50 * screenScaling || paddedBounds.Width > 84 * screenScaling)
            {
                // more than 3 characters in a box too large is likely going to be good, pass it but mark as potentially bad
                if (word.Length > 3)
                {
                    validity = WordMatchValidity.TooLargeButEnoughCharacters;
                }
                else
                {
                    validity = WordMatchValidity.TooLarge;
                }
            }
            else if (word.Length < 2 && Settings.locale == "en")
            {
                validity = WordMatchValidity.TooFewCharacters;
            }
            else
            {
                validity = WordMatchValidity.GoodMatch;
            }
        }

        public static List<InventoryItem> ToInventoryItems(List<WordMatch> matches)
        {
            List<WordMatch> filteredMatches = matches.Where(match => (int)match.validity <= 100).ToList(); //filter only valid matches
            List<Tuple<List<WordMatch>, Rectangle>> matchedMatches = MatchMatches(filteredMatches);

            List<InventoryItem> results = new List<InventoryItem>();
            foreach (Tuple<List<WordMatch>, Rectangle> itemGroup in matchedMatches)
            {
                string name = WordMatch.GetCombinedName(itemGroup.Item1);
                results.Add(new InventoryItem(name, itemGroup.Item2));
            }

            return results;
        }

        public static List<Tuple<List<WordMatch>, Rectangle>> MatchMatches(List<WordMatch> input)
        {
            List<Tuple<List<WordMatch>, Rectangle>> matchedMatches = new List<Tuple<List<WordMatch>, Rectangle>>(); //List containing Tuples of overlapping InventoryItems and their combined bounds
            foreach (WordMatch match in input)
            {
                int i = matchedMatches.FindLastIndex((item) => item.Item2.IntersectsWith(match.paddedBounds)); //find last item that intersects with our match

                if (i == -1)
                {
                    //New entry added by creating a tuple. Item1 in tuple is list with just the newly found item, Item2 is its bounds
                    matchedMatches.Add(Tuple.Create(new List<WordMatch> { match }, match.paddedBounds));
                }
                else
                {
                    Tuple<List<WordMatch>, Rectangle> matchesTuple = matchedMatches[i];
                    matchedMatches.RemoveAt(i);

                    List<WordMatch> newMatchList = matchesTuple.Item1;
                    newMatchList.Add(match);
                    Rectangle combinedBounds = Util.CombineBounds(matchesTuple.Item2, match.paddedBounds);

                    matchedMatches.Add(Tuple.Create(newMatchList, combinedBounds));
                }
            }

            return matchedMatches;
        }

        public static string GetCombinedName(List<WordMatch> matches)
        {
            matches.Sort(SortByBounds);

            string name = string.Empty;
            foreach (WordMatch match in matches)
            {
                name += (match.word + " ");
            }

            return name.Trim();
        }

        //Sort order for component words to appear in. If large height difference, sort vertically. If small height difference, sort horizontally
        public static int SortByBounds(WordMatch m1, WordMatch m2)
        {
            return Math.Abs(m1.paddedBounds.Top - m2.paddedBounds.Top) > m1.paddedBounds.Height / 8
                ? m1.paddedBounds.Top - m2.paddedBounds.Top
                : m1.paddedBounds.Left - m2.paddedBounds.Left;
        }
    }
}