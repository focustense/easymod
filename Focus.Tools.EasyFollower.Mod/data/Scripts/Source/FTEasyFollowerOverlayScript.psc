Scriptname FTEasyFollowerOverlayScript extends Actor

;Constants
int Property KEY_TEXTURE = 9 AutoReadOnly
int Property KEY_TINT_COLOR = 7 AutoReadOnly
int Property KEY_TINT_ALPHA = 8 AutoReadOnly
int Property KEY_EMISSIVE_COLOR = 0 AutoReadOnly
int Property KEY_EMISSIVE_MULTIPLE = 1 AutoReadOnly

;Script properties
string[] Property NodeNames Auto
;Node index isn't part of the node name e.g. "[Ovl0]" vs. "[Ovl1]".
;Rather, it's the index argument provide with AddOverrideString for the texture path, i.e. for key 9.
;It's unclear why this should be anything other than 0, but RaceMenu supports it.
;Even less clear what it would mean if the original jslot had multiple index overrides on the same node.
;We just assume that there's 1 texture per node (which is actually multiple per "body part"), as this is
;how the RaceMenu UI works anyway.
int[] Property NodeIndices Auto
string[] Property TexturePaths Auto
;Colors are ARGB
int[] Property TintColors Auto
int[] Property EmissiveColors Auto

Function AddOverlay(Actor target, string nodeName, int index, string texturePath, int tintColor, int emissiveColor)
	Debug.Trace("Node " + nodeName + " has texture [" + texturePath + "] with tint=" + tintColor + ", emissive=" + emissiveColor)
	bool isFemale = target.GetActorBase().GetSex() == 1
	int tintRgb = Math.LogicalAnd(tintColor, 0x00ffffff)
	int tintAlpha = Math.RightShift(tintColor, 24)
	int emissiveRgb = Math.LogicalAnd(emissiveColor, 0x00ffffff)
	int emissiveAlpha = Math.RightShift(emissiveColor, 24)
	If (tintAlpha == 0 && emissiveAlpha == 0)
		Debug.Trace("Ignoring invisible override")
		return
	EndIf
	Debug.Trace("Applying override...")
	NiOverride.AddNodeOverrideString(target, isFemale, nodeName, KEY_TEXTURE, index, texturePath, ;/ persist /; true)
	NiOverride.AddNodeOverrideInt(target, isFemale, nodeName, KEY_TINT_COLOR, -1, tintRgb, ;/ persist /; true)
	NiOverride.AddNodeOverrideFloat(target, isFemale, nodeName, KEY_TINT_ALPHA, -1, tintAlpha / 255.0, ;/ persist /; true)
	If (emissiveAlpha > 0)  ; Usually not used, don't waste script cycles on it
		NiOverride.AddNodeOverrideInt(target, isFemale, nodeName, KEY_EMISSIVE_COLOR, -1, emissiveRgb, ;/ persist /; true)
		NiOverride.AddNodeOverrideFloat(target, isFemale, nodeName, KEY_EMISSIVE_MULTIPLE, -1, emissiveAlpha / 10.0, ;/ persist /; true)
	EndIf
	NiOverride.AddOverlays(target)
	Debug.Trace("Successfully applied override")
EndFunction

Event OnLoad()
	Debug.Trace("Restoring overlays for NPC: " + Self.GetActorBase().GetName())
	If (SKSE.GetPluginVersion("skee") < 1)
		Debug.Trace("RaceMenu plugin is missing or invalid. Overlays will be ignored.")
		return
	EndIf
	Debug.Trace("Found " + NodeNames.Length + " node overrides")
	int i = 0
	While (i < NodeNames.Length)
		AddOverlay(Self, NodeNames[i], NodeIndices[i], TexturePaths[i], TintColors[i], EmissiveColors[i])
		i += 1
	EndWhile
EndEvent
