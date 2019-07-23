Feature: MiddlewareReceiveTicketUpdates
	As Middleware
	I want to receive updates to tickets
	So that I can distribute the updates to other systems

@SkipWhenLiveUnitTesting
Scenario: Share ticket with middleware
	Given a ticket exists
	When the ticket is marked to be shared 
	Then the ticket is shared with the Middleware
	And the ticket is not marked to be shared

