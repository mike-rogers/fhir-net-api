﻿/* 
 * Copyright (c) 2016, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/ewoutkramer/fhir-net-api/master/LICENSE
 */

using Hl7.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Navigation;
using Hl7.Fhir.Support;
using System;
using System.Linq;

namespace Hl7.Fhir.Validation
{
    internal static class ChildConstraintValidationExtensions
    {
        internal static OperationOutcome ValidateChildConstraints(this Validator validator, ElementDefinitionNavigator definition, IElementNavigator instance)
        {
            var outcome = new OperationOutcome();
            if (!definition.HasChildren) return outcome;

            validator.Trace(outcome, "Start validation of inlined child constraints for '{0}'".FormatWith(definition.Path), Issue.PROCESSING_PROGRESS, instance);

            var matchResult = ChildNameMatcher.Match(definition, instance);

            if (matchResult.UnmatchedInstanceElements.Any())
            {
                var elementList = String.Join(",", matchResult.UnmatchedInstanceElements.Select(e => "'" + e.Name + "'"));
                validator.Trace(outcome, $"Encountered unknown child elements {elementList} for definition '{definition.Path}'",
                                Issue.CONTENT_ELEMENT_HAS_UNKNOWN_CHILDREN, instance);
            }

            //TODO: Give warnings for out-of order children.  Really? That's an xml artifact, no such thing in Json!
            //(with the serializationrepresentationnav we could determine the source is xml and make order matter....)

            // Recursively validate my children
            foreach (var match in matchResult.Matches)
            {
                outcome.Add(validator.ValidateMatch(match, instance));
            }

            return outcome;
        }

        private static OperationOutcome ValidateMatch(this Validator validator, Match match, IElementNavigator parent)
        {
            var outcome = new OperationOutcome();

            var definition = match.Definition.Current;

            if (definition.Min == null)
                validator.Trace(outcome, $"Element definition does not specify a 'min' value, which is required. Cardinality has not been validated",
                    Issue.PROFILE_ELEMENTDEF_CARDINALITY_MISSING, parent);
            else if (definition.Max == null)
                validator.Trace(outcome, $"Element definition does not specify a 'max' value, which is required. Cardinality has not been validated",
                    Issue.PROFILE_ELEMENTDEF_CARDINALITY_MISSING, parent);

            var cardinality = Cardinality.FromElementDefinition(definition);

            var bucket = BucketFactory.CreateRoot(match.Definition, validator);

            foreach (var element in match.InstanceElements)
            {
                var matchOutcome = bucket.Add(element);

                // For the "root" slice group (=the original core element that was sliced, not resliced)
                // any element that does not match is an error
                if (!matchOutcome.Success)
                {
                    outcome.Add(matchOutcome);
                }
            }

            outcome.Add(bucket.Validate(validator, parent));

            return outcome;
        }
    }
}
