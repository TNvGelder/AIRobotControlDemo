Robots should be able to chat through MCP.
  Please set everything up and include chat bubbles and test the backend.
  Max 1 message per second.

  You will need to add signalr for communication between backend and frontend.

  When robots decide to fight another robot, they should fight by shooting lasers towards eachother.

  Extra context for prompt: Which personas are within chatting range

  There should be MCP actions as follows:
  MCP -> Persona overview
  MCP -> Get persona details
  MCP -> GetOverviewForPersona
      - Includes chathistory with messages that have been visible to the persona
      - Should include which personas are in talkingdistance, and events if a persona has come within
  talkingrange or left talkingrange.
      - Locations of objects on the map
  MCP -> Send message for specific persona
  MCP -> GetChatDataForPersona -> Should include which personas are in talkingdistance, and events if a
  persona has come within talkingrange or left talkingrange.
  MCP -> AttackAnotherPersona
  MCP -> WalkTowards (persona, battery, coordinates)
  MCP -> SetMission(Persona, other persona)
  MCP -> Follow (persona, other persona)
  MCP -> Patrol (walk between 2 coordinates)
  MCP -> UpdateHappiness(persona)
  MCP -> Switch groups (persona/robot switches to another group)

  Add batteries on the map too that respawn and that robots can retrieve to fill their energy. This is to
  create a resource to compete over. Make sure that the batteries dont fully run out/restock but are scarce
  enough to cause conflict between groups too.
   Use context7 for researching documentation and @Docs\Backend-Vertical-Slices.md in robots you can find the
   unfinished 3d demo which also needs updates   @airobotcontrol.client\src\demos\robots\. Make sure you get
  a working project FE and BE.