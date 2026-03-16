#if false
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Xunit;
using Xunit;
using WorkTicketApp.Models;
using Xunit;
using System.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema.Annotations;

namespace WorkTicketApp.Tests
{
    public class WorkTicketTests
    {
        private IList<ValidationResult> ValidateModel(object model)
        {
            var validationResults = new List<ValidationResult>();
            var ctx = new ValidationContext(model, null, null);
            Validator.TryValidateObject(model, ctx, validationResults, true);
            return validationResults;
        }

        [Fact]
        public void WorkTicket_ShouldBeValid_WhenAllRequiredFieldsArePresentAndCountersAreValid()
        {
            // Arrange
            var ticket = new WorkTicket
            {
                TicketNumber = "TKT-001",
                CostCentre = "CC-01",
                StartCounter = 100,
                EndCounter = 200 // Greater than StartCounter
            };

            // Act
            var validationResults = ValidateModel(ticket);

            // Assert
            Assert.Empty(validationResults);
        }

        [Fact]
        public void WorkTicket_ShouldBeInvalid_WhenTicketNumberIsMissing()
        {
            // Arrange
            var ticket = new WorkTicket
            {
                TicketNumber = null, // Required
                CostCentre = "CC-01",
                StartCounter = 100,
                EndCounter = 200
            };

            // Act
            var validationResults = ValidateModel(ticket);

            // Assert
            Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(WorkTicket.TicketNumber)));
        }

        [Fact]
        public void WorkTicket_ShouldBeInvalid_WhenEndCounterIsNotGreaterThanStartCounter()
        {
            // Arrange
            var ticket = new WorkTicket
            {
                TicketNumber = "TKT-002",
                CostCentre = "CC-02",
                StartCounter = 500,
                EndCounter = 400 // Less than StartCounter
            };

            // Act
            var validationResults = ValidateModel(ticket);

            // Assert
            Assert.Contains(validationResults, v => 
                v.MemberNames.Contains(nameof(WorkTicket.EndCounter)) && 
                v.ErrorMessage.Contains("End Counter must be greater than Start Counter"));
        }
    }
}
#endif