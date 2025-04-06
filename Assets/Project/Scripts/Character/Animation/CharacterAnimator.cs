using UnityEngine;
using System.Collections.Generic;

public class CharacterAnimator : MonoBehaviour
{
    // Sprites for each body part
    public SpriteRenderer headRenderer;
    public SpriteRenderer bodyRenderer;
    public SpriteRenderer hairRenderer;
    public SpriteRenderer torsoRenderer;
    public SpriteRenderer legsRenderer;


    // Equipment data for different parts
    public SO_EquipmentData headData;
    public SO_EquipmentData bodyData;
    public SO_EquipmentData hairData;
    public SO_EquipmentData torsoData;
    public SO_EquipmentData legsData;

    // Currently equipped item numbers (used to identify the correct sprite set)
    public int headItemNumber = 20001;
    public int bodyItemNumber = 10001;
    public int hairItemNumber = 30001;
    public int torsoItemNumber = 40001;
    public int legsItemNumber = 50001;


    // Current animation state
    private int currentFrame;
    private float frameTimer;
    public float frameRate = 0.1f; // 10 frames per second

    private string currentAnimation;

    // Animation Frames Dictionary
    private readonly Dictionary<string, (int startFrame, int endFrame)> animationFrames = new Dictionary<string, (int, int)>
    {
        { "spellcast_up", (0, 6) },
        { "spellcast_left", (13, 19) },
        { "spellcast_down", (26, 32) },
        { "spellcast_right", (39, 45) },
        { "thrust_up", (52, 59) },
        { "thrust_left", (65, 72) },
        { "thrust_down", (78, 85) },
        { "thrust_right", (91, 98) },
        { "walk_up", (104, 112) },
        { "walk_left", (117, 125) },
        { "walk_down", (130, 138) },
        { "walk_right", (143, 151) },
        { "slash_up", (156, 161) },
        { "slash_left", (169, 174) },
        { "slash_down", (182, 187) },
        { "slash_right", (195, 200) },
        { "shoot_up", (208, 220) },
        { "shoot_left", (221, 233) },
        { "shoot_down", (234, 246) },
        { "shoot_right", (247, 259) },
        { "hurt", (260, 265) },
        { "climb", (273, 278) },
        { "idle_up", (286, 287) },
        { "idle_left", (299, 300) },
        { "idle_down", (312, 313) },
        { "idle_right", (325, 326) },
        { "combat_up", (288, 289) },
        { "combat_left", (301, 302) },
        { "combat_down", (314, 315) },
        { "combat_right", (327, 328) },
        { "jump_up", (338, 342) },
        { "jump_left", (351, 355) },
        { "jump_down", (364, 368) },
        { "jump_right", (377, 381) },
        { "sit_up", (390, 392) },
        { "sit_left", (403, 405) },
        { "sit_down", (416, 418) },
        { "sit_right", (429, 431) },
        { "emote_up", (393, 395) },
        { "emote_left", (406, 408) },
        { "emote_down", (419, 421) },
        { "emote_right", (432, 434) },
        { "run_up", (442, 449) },
        { "run_left", (455, 462) },
        { "run_down", (468, 475) },
        { "run_right", (481, 488) },
        { "one_hand_halfslash_up", (500, 502) },
        { "one_hand_halfslash_left", (513, 515) },
        { "one_hand_halfslash_down", (526, 528) },
        { "one_hand_halfslash_right", (539, 541) },
        { "one_hand_backslash_up", (494, 506) },
        { "one_hand_backslash_left", (507, 519) },
        { "one_hand_backslash_down", (520, 532) },
        { "one_hand_backslash_right", (533, 545) },
        { "one_hand_slash_up", (546, 551) },
        { "one_hand_slash_left", (559, 564) },
        { "one_hand_slash_down", (572, 577) },
        { "one_hand_slash_right", (585, 590) }
    };

    private int currentAnimationStart;
    private int currentAnimationEnd;
    private bool loopAnimation;

    private void Start()
    {
        PlayIdle(PlayerFacing.Down);
    }

    private void Update()
    {
        UpdateAnimation();
    }

    /// Applies character state from NetworkCharacter.
    public void ApplyCharacterState(CharacterState state, PlayerFacing direction)
    {
        if (state == CharacterState.Running)
        {
            PlayRun(direction);
        }
        else
        {
            PlayIdle(direction);
        }
    }

    /// Plays an idle animation based on facing direction.
    public void PlayIdle(PlayerFacing facing)
    {
        PlayAnimation($"idle_{facing.ToString().ToLower()}", true);
    }

    /// Plays a run animation based on facing direction.
    public void PlayRun(PlayerFacing facing)
    {
        PlayAnimation($"run_{facing.ToString().ToLower()}", true);
    }

    /// Core function to play an animation by name.
    public void PlayAnimation(string animationName, bool looping)
    {
        // Prevent restarting the same animation
        if (currentAnimation == animationName) return;

        // Store the new animation name as the current one
        currentAnimation = animationName;
        
        // Try to get the frame range for this animation from the dictionary
        if (!animationFrames.TryGetValue(animationName, out var frames))
        {
            // If animation name not found, log an error and exit
            Debug.LogError($"Animation '{animationName}' not found.");
            return;
        }

        currentFrame = frames.startFrame;       // Set the starting frame for the animation
        frameTimer = 0f;    // Reset the frame timer so it starts fresh for this animation
        loopAnimation = looping;     // Store whether the animation should loop or not
        currentAnimationStart = frames.startFrame;     // Store the start frames for the animation
        currentAnimationEnd = frames.endFrame;    // Store the end frames for the animation

        SetFrame(currentFrame); // Show the first frame immediately
    }

    private void UpdateAnimation()
    {
        // Add the time passed since the last frame to the frame timer
        // This helps control the speed of animation playback
        frameTimer += Time.deltaTime;

        // Check if enough time has passed to move to the next frame
        if (frameTimer >= frameRate)
        {
            // Subtract the frameRate from the timer to account for frame advance
            // (This keeps leftover time so it's still accurate over time)
            frameTimer -= frameRate;
            currentFrame++; // Advance to the next frame in the animation sequence

            // If the current frame has gone past the last frame in the animation
            if (currentFrame > currentAnimationEnd)
            {
                // If this animation should loop, reset to the first frame
                if (loopAnimation)
                {
                    currentFrame = currentAnimationStart;
                }
                // Otherwise, stop at the last frame and don't advance further
                else
                {
                    currentFrame = currentAnimationEnd;
                }
            }
            // Apply the new frame by updating the sprite renderers
            SetFrame(currentFrame);
        }
    }

    /// Sets the correct frame for each body part.
    public void SetFrame(int frameIndex)
    {
        // Update the current frame tracker to the given frame index
        currentFrame = frameIndex;

        // For each body part, if its SpriteRenderer and SO_EquipmentData are assigned,
        // fetch the correct sprite from the equipment data and set it to the renderer

        if (headRenderer != null && headData != null)
            headRenderer.sprite = GetSpriteFromItem(headData, headItemNumber, frameIndex); // Set head sprite

        if (bodyRenderer != null && bodyData != null)
            bodyRenderer.sprite = GetSpriteFromItem(bodyData, bodyItemNumber, frameIndex); // Set body sprite

        if (hairRenderer != null && hairData != null)
            hairRenderer.sprite = GetSpriteFromItem(hairData, hairItemNumber, frameIndex); // Set hair sprite

        if (torsoRenderer != null && torsoData != null)
            torsoRenderer.sprite = GetSpriteFromItem(torsoData, torsoItemNumber, frameIndex); // Set torso sprite

        if (legsRenderer != null && legsData != null)
            legsRenderer.sprite = GetSpriteFromItem(legsData, legsItemNumber, frameIndex);  // Set legs sprite
    }

    /// Fetches the correct sprite for an equipment item.
    private Sprite GetSpriteFromItem(SO_EquipmentData data, int itemNumber, int frameIndex)
    {
        // Loop through all equipment items in the given data (e.g., all head items)
        foreach (var item in data.equipmentItems)
        {
            // Check if this item matches the requested item number (equipped item)
            if (item.itemNumber == itemNumber)
            {
                // Check if the requested frame index is within the bounds of the sprite array
                if (frameIndex >= 0 && frameIndex < item.slicedSpritesArray.Length)
                    // Return the sprite corresponding to the frame index
                    return item.slicedSpritesArray[frameIndex];
                else
                    // Log a warning if the frame index is invalid for this item
                    Debug.LogWarning($"Frame index {frameIndex} is out of range for item {item.itemName}");
            }
        }
        // If no item matched the item number, log a warning
        Debug.LogWarning($"Item number {itemNumber} not found in {data.name}");
            
        // Return null to indicate failure to find the sprite
        return null;
    }

    /// Refreshes the character's current animation frame.
    public void RefreshCurrentFrame()
    {
        // Re-applies the current frame by calling SetFrame()
        // Useful when equipment or sprite data changes but the animation/frame stays the same
        SetFrame(currentFrame);
    }
}
