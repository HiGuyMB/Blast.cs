//-----------------------------------------------------------------------------
// Blast.cs
//
// Hax central, but works
//
// Implemented for any Marble Blast; any version
// So long as a few things are kept similar
//
// From Project Revolution
// The MultiPlayer MarbleBlast experience
//-----------------------------------------------------------------------------

//-----------------------------------------------------------------------------
// Copyright(c) 2011-2012 HiGuy Smith
// Copyright(c) 2011-2012 Jeff Hutchinson
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//-----------------------------------------------------------------------------

//This is so we can wait for the game to finish inializing
if ($BlastExec == false) {
   $BlastExec = true;
   //Wait about one second
   //As MB is non-threaded, this will wait
   //for all the scripts to load from the
   //initialzation processes
   schedule(1000, 0, "exec", $Con::File);
   return;
}

//Determine mod
function getMod() {
   if (isFile("marble/server/scripts/speedmeter.cs.dso"))
      return "Advanced";
   if (isFile("marble/client/ui/Awards/AllSapphires.png"))
      return "Emerald";
   if (isFile("marble/quickFix.cs"))
      return "Future";
   if (isFile("marble/quickFix.cs.dso"))
      return "Future";
   if (isFile("marble/client/ui/fullVersion.png"))
      return "Gold Demo";
   if (isFile("marble/client/ui/STBox.gui.dso"))
      return "Opal";
   if (isFile("marble/client/ui/play_rc_d.png"))
      return "Platinum";
   if (isFile("platinum/client/ui/play_rc_d.png"))
      return "Platinum";
   if (isFile("marble/client/ui/BBLoadingGui.gui.dso"))
      return "Reloaded";
   if (isFile("marble/data/particles/debris.png"))
      return "Revived";
   if (isFile("marble/client/ui/loading/loadinggui_origin.png"))
      return "Space";
   return "Gold";
}

echo("Loading blast scripts into Marble Blast" SPC getMod() @ "...");

//-----------------------------------------------------------------------------
// PlayGui "Blast Bar"
// Adds to PlayGui just like this
// Kind of hackish, but it works

if (!isObject(GuiBlastDisabledProfile)) {
   new GuiControlProfile(GuiBlastDisabledProfile) {
      opaque = false;
      fillColor = "152 152 152 100";
      border = true;
      borderColor = "78 88 120";
   };
}

if (!isObject(GuiBlastEnabledProfile)) {
   new GuiControlProfile(GuiBlastEnabledProfile) {
      opaque = false;
      fillColor = "44 152 162 100";
      border = true;
      borderColor   = "78 88 120";
   };
}
function addPGBlastBar() {
   if (!isObject(PG_BlastBar))
      PlayGui.add(
         new GuiProgressCtrl(PG_BlastBar) {
            profile = "GuiBlastDisabledProfile";
            horizSizing = "right";
            vertSizing = "top";
            position = "12" SPC getWord(PlayGui.extent, 1) - 78;
            extent = "96 21";
            minExtent = "8 8";
            visible = "1";
            helpTag = "0";
         }
      );
}

addPGBlastBar();

package BlastOverrides {
   function playDemo(%file) {
      deactivatePackage(BlastOverrides);
      $PlayingDemoFile = %file;
      playDemo(%file);
      activatePackage(BlastOverrides);
   }
   function onServerCreated() {
      deactivatePackage(BlastOverrides);
      onServerCreated();
      makeBlastDatablocks();
      activatePackage(BlastOverrides);
   }
};

activatePackage(BlastOverrides);

//-----------------------------------------------------------------------------
// Game Start Notifications
// This method was empty in MBG, MBP,
// MBE, MBO, and any other mod I could lay hands on the console of

function clientCmdGameStart() {
   //Reset variables
   $BlastGameDidStart = true;
   if ($playingDemo) {
      //blastReadIn();
      $BlastDemoCurrent = 0;
   } else {
      if ($BlastRecordingDemo) {

      }
   }
}

//-----------------------------------------------------------------------------
// Blast recording in/out

function blastReadIn() {
   deleteVariables("$BlastDemo*");
   %file = filePath($playingDemoFile) @ "/" @  fileBase($playingDemoFile) @ ".bls";
   echo("Reading in blasts for recording from" SPC %file);
   %in = new FileObject();
   if (!%in.openForRead(%file)) {
      error("Could not read in blast scripts for demo play!");
      error("The demo will omit any blasts that were used.");
      error("This will potentially ruin any runs with blasts.");
      %in.close();
      %in.delete();
      return;
   }
   //Read in variables
   %lines = 0;
   while (!%in.isEOF()) {
      %line = trim(%in.readLine());
      $BlastDemoTime[%lines] = %line;
      %lines ++;
   }
   $BlastDemoCurrent = 0;
   %in.close();
   %in.delete();
}

function blastWriteOut() {
   %file = $usermods @ "/client/demos/demo.bls";
   if (getMod() $= "Platinum")
      %file = $usermods @ "/client/demos/" @ $recordDemoName @ ".bls";
   echo("Writing out blasts for recording to" SPC %file);
   %out = new FileObject();
   if (!%out.openForWrite(%file)) {
      error("Could not write out blast scripts for recording!");
      error("The recording will omit any blasts that were used.");
      error("This will potentially ruin any runs with blasts.");
      %out.close();
      %out.delete();
      return;
   }
   //Write out variables
   for (%i = 0; %i < $BlastTimes; %i ++) {
      %out.writeLine($BlastTime[%i]);
   }
   %out.close();
   %out.delete();
}

//-----------------------------------------------------------------------------
// Blast Update Stuff

$BlastRequiredAmount = 0.6;

function blastUpdate() {
   //Calculate the passed time since last update
   %timeDelta = getRealTime() - $LastBlastUpdate;
   //Reset update time
   $LastBlastUpdate = getRealTime();
   //In case this was not added
   if (!isObject(PG_BlastBar))
      addPGBlastBar();
   //If the game ends...
   if ($BlastGameDidStart && ($Game::Running == false || !isObject(ServerConnection))) {
      //Unset this
      $BlastGameDidStart = false;
      //If we are recording
      if ($BlastRecordingDemo && !$playingDemo) {
         //Write out blasts
         //blastWriteOut();
      }
   }
   //Only update while playing!
   if (isObject(LocalClientConnection)) {
      PG_BlastBar.setProfile(LocalClientConnection.blastValue >= $BlastRequiredAmount ? GuiBlastEnabledProfile : GuiBlastDisabledProfile);
   }
   if ($PlayTimerActive) {
      $BlastRecordingDemo = $doRecordDemo;
      //Auto-blast if demo
      if ($PlayingDemo) {
         //Check if we should blast
         if ($BlastDemoTime[$BlastDemoCurrent]) {
            %nextBlast = $BlastDemoTime[$BlastDemoCurrent];
            //Only blast if time >= when we are now
            if (PlayGui.elapsedTime >= %nextBlast) {
               //If yes, easy line
               blast();
               //Inc the counter so we don't forever blast :)
               $BlastDemoCurrent ++;
            }
         }
      }
      //Update blast value
      LocalClientConnection.blastValue += (%timeDelta / 10000);
      //Normalize blast value
      //Keep it 0 < value < 1
      if (LocalClientConnection.blastValue < 0)
         LocalClientConnection.blastValue = 0;
      if (LocalClientConnection.blastValue > 1)
         LocalClientConnection.blastValue = 1;
      //Display value
      PG_BlastBar.setValue(LocalClientConnection.blastValue);
   }
   //Continue the loop
   //Usually, 33.3 fps is all that is required for smooth updating
   //But this can be adjusted
   schedule(30, 0, "blastUpdate");
}

//Start the loop
blastUpdate();

//-----------------------------------------------------------------------------
// OnMissionReset scripts
// Needed for reset of blast
// Works for both OOB and restart button

function GameConnection::onMissionReset(%this)
{
   //Delete .rec varibles
   deleteVariables("$BlastTim*");
   $BlastTimes = 0;
   //Reset value
   %this.blastValue = 0;
   //Display value
   PG_BlastBar.setValue(%this.blastValue);
}

//Here, the Sky is used because it is always present
function Sky::onMissionReset(%this) {
   ClientGroup.onMissionReset();
}

//-----------------------------------------------------------------------------
// Blast function
// Where the knitty gritty is done
// %val is keyReleased,
// NOT blast value, h4x0rz

function blast() {
   echo("Blasting!");
   //Make sure it is high enough
   if (LocalClientConnection.blastValue >= $BlastRequiredAmount) {
      if ($BlastRecordingDemo && !$playingDemo) {
         //Add to list for .rec files
         $BlastTime[$BlastTimes] = PlayGui.elapsedTime;
         $BlastTimes ++;
      }
      //Best results found when whacked from here
      %attack = "0 0 -1";
      //Confusing, but all this does is set the impulse
      //to the blast value shown * 10 and then adjusted
      //to the gravity (so we don't get blasted sideways
      //after a gravity modifier)
      %push = VectorMult(LocalClientConnection.blastValue * -8.5 SPC LocalClientConnection.blastValue * -8.5 SPC LocalClientConnection.blastValue * -8.5, getGravityDir());
      //Get the local marble, as impulsing the server one
      //will reset the camera angle; we don't want that
      findRealMarble().applyImpulse(%attack, %push);
      //Display blast particles
      makeBlastParticle();
      //Finally, reset
      LocalClientConnection.blastValue = 0;
      PG_BlastBar.setValue(LocalClientConnection.blastValue);
   } else {
      echo("Not enough blast!");
   }
}

// UseBlast Function
// Just so we can cancel user activation in a demo

function useBlast(%val) {
   if ($playingDemo)
      return;
   if (%val)
      blast();
}

//-----------------------------------------------------------------------------
// Blast Particle
//-----------------------------------------------------------------------------

function makeBlastDatablocks() {
   // This is the actual particle that is being emitted
   datablock ParticleData(BlastSmoke) {
      //Texture
      textureName          = "~/data/particles/smoke.png";
      //I don't know, really
      dragCoefficient      = 1;
      //So it doesn't fall
      gravityCoefficient   = 0;
      //So it doesn't speed up
      inheritedVelFactor   = 0;
      //So it doesn't speed up
      windCoefficient      = 0;
      //So it doesn't speed up
      constantAcceleration = 0;
      //Only last a little bit
      lifetimeMS           = 500;
      //Maybe a little variation
      lifetimeVarianceMS   = 100;
      //A little spinny
      spinSpeed     = 20;
      //SPIN THE SAME
      spinRandomMin = 0.0;
      //SPIN THE SAME
      spinRandomMax = 0.0;
      //No clue, works though
      useInvAlpha   = true;

      //Colors for each key
      colors[0]     = "0 1 0.4 0.0";
      colors[1]     = "0 1 0.4 0.5";
      colors[2]     = "0 1 0.4 0.9";

      //Sizes for each key
      sizes[0]      = 0.5;
      sizes[1]      = 0.5;
      sizes[2]      = 0.5;

      //Times for each key
      times[0]      = 0.0;
      times[1]      = 0.4;
      times[2]      = 1.0;
   };

   // This is the emitter that emits the blast particles
   datablock ParticleEmitterData(BlastEmitter) {
      //Spawn a new particle every 30ms
      ejectionPeriodMS = 30;
      //Spawn them without random delay between
      periodVarianceMS = 0;
      //Eject them somewhat speedily
      ejectionVelocity = 4;
      //Velocities must be equal to look cool
      velocityVariance = 0;
      //No idea, leave at 0
      ejectionOffset   = 0;
      //Min up-down. 90 is straight out, 0 is straight up
      thetaMin         = 90;
      //Max up-down. 180 is straight down, 270 is behind you
      thetaMax         = 100;
      //Minimum velocity rotation in a circle
      phiReferenceVel  = 0;
      //Maximum velocity rotation in a circle
      phiVariance      = 360;
      //Lifetime of the emitter
      lifetimeMS       = 500;
      //Particles
      particles = "BlastSmoke";
   };

   // This is the emitter node that is used in the mission
   datablock ParticleEmitterNodeData(BlastNode) {
      //Time scaled by one (no effect)
      timeMultiple = 1;
   };
}

function makeBlastParticle() {
   //If we can blast (no sneakies)
   if (LocalClientConnection.blastValue >= $BlastRequiredAmount) {
      //Set lifetime of particle effect
      //Less power means fewer particles
      BlastEmitter.lifetimeMS = mPow(LocalClientConnection.blastValue * 20, 2);
      //Get position for particles
      %position = getWords(LocalClientConnection.player.getTransform(), 0, 2);
      //Get rotation for particles
      %rotation = getWords(LocalClientConnection.player.getTransform(), 3);
      //Make the particle emitter
      %emitter = new ParticleEmitterNode() {
         //Particle emitter node datablock
         datablock = BlastNode;
         //Particle emitter datablock
         emitter = BlastEmitter;
         //Position
         position = %position;
         //Rotation
         rotation = %rotation;
      };
      //Delete emitter after a little bit
      //To not clog up missioncleanup
      //and therefore the editor
      %emitter.schedule(BlastEmitter.lifeTimeMS * 2, "delete");
      //And finally, add the particles
      MissionCleanup.add(%emitter);
   }
}

//*****************************************************************************
//*****************************************************************************

//-----------------------------------------------------------------------------
// Support Functions
// These are just generally useful

//-----------------------------------------------------------------------------
// Because the cursor dies, no idea why

function ExitGameDlg::onWake(%this) {
   cursorOn(); //CURSOR ON!
   //Y U NO TURN ON?
}

//-----------------------------------------------------------------------------
// Find the client-side marble of LocalClientConnection
// Adapted from the original to be single-player only

function findRealMarble() {
   //Iterate through all objects in client-side server connection
   for (%i = 0; %i < ServerConnection.getCount(); %i ++) {
      //Get the object from the iteration
      %obj = ServerConnection.getObject(%i);
      //Check for ID. The marble ID will *always* be higher.
      //Analysis shaped by using tree();
      //tree(); is one of those hidden-but-useful functions
      if (%obj.getId() < LocalClientConnection.player.getId())
         continue;
      //If it's a marble, then we're good!
      //This is *guarenteed* to be the client-side marble, 100%
      //of the time, if you are playing single player
      if (%obj.getClassName() $= "Marble") {
         return %obj;
      }
   }
}

//-----------------------------------------------------------------------------
// Return the maximum of two numbers
// Just because I'm lazy
// No in-depth explanation required
// and none will be given

function max(%a, %b) {
   if (%a > %b)
      return %a;
   return %b;
}

//-----------------------------------------------------------------------------
// Vector Multiplying
// Works for any size vectors
// Find on Marble Blast Forums in my scripts thread
// http://marbleblast.com/index.cgi?board=mbdkcode&action=display&thread=12703

function VectorMult(%vec1, %vec2) {
   %finished = "";
   //Iterate through all the dimensions of the two vectors
   //The count is of length of whichever vector is longer
   for (%i = 0; %i < max(getWordCount(%vec1), getWordCount(%vec2)); %i ++) {
      //If %i, then %i > 0, and %finished will not be ""
      if (%i) {
         //Append dimension
         %finished = %finished SPC getWord(%vec1, %i) * getWord(%vec2, %i);
      } else {
         //Set %finished to dimension
         %finished = getWord(%vec1, %i) * getWord(%vec2, %i);
      }
   }
   return %finished;
}

//-----------------------------------------------------------------------------
// Blast Binding
// Changable in config.cs

function addBlastBinding() {
   //If binding exists, don't replace it!
   if (moveMap.getBinding("blast") $= "")
      moveMap.unbind(keyboard, "b");
   if (moveMap.getBinding("useblast") $= "")
      moveMap.bind(keyboard, "b", useblast);
}

addBlastBinding();

//-----------------------------------------------------------------------------

// End of blast.cs
