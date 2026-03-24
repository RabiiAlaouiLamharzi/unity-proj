# VRSelfEmpathy

### Introduction

Mental health and emotional well-being are increasingly recognized as important areas where technology can help. This project explores how Virtual Reality (VR) can support self-empathy (the ability to be kind to yourself).

We built a VR experience where the user plays the role of a caring adult comforting a sad little girl, who is actually a younger version of themselves. The idea is that by physically interacting with this child character (stroking her head, giving her a teddy bear, squatting down to her level, speaking kind words), the user practices compassion toward their past self.

The application was built in Unity for a head-mounted VR headset (HTC Vive Cosmos) and makes use of physical interactions, animated characters, voice recording, and spatial audio to create an emotionally engaging experience.

### Project Overview & Features

- VR self-empathy app (user comforts a sad child version of themselves)
- Phase 1 (adult POV) + Phase 2 (child POV swap)
- Animated girl character with 10+ animations (Crying, Sad, Happy, Hug, Talking/Argue, Idle, Happy-Idle, etc.)
- IK rig on the character (hands and head track in real time)
- Step-by-step dialogue flow with speech bubbles and narrator prompts
- Voice recording (user speaks to the child character + hears their own voice played back in Phase 2 from the child's perspective)
- 3D audio: giggle, success sound, crying/sobbing clips triggered at specific moments
- Particle effects (sparkles on happy moments)
- Physical VR interactions: head stroking (touch sensor on girl's head), proximity-based auto-grab of teddy bear (attaches to right controller when close), auto-transfer of bear to girl when user walks near her, squat detection via HMD height drop

### Requirements Coverage

- **YES** VR application for health & well-being (self-empathy)
- **YES** Basic manipulation: head stroking (touch sensor on girl's head), proximity-based auto-grab of teddy bear (attaches to right controller when close), auto-transfer of bear to girl when user walks near her, squat detection via HMD height drop
- **NO** Navigation (locomotion/teleportation)
- **YES** Multiple Unity scenes linked in a flow (Phase 1 → Phase 2 POV swap)
- **YES** 3D audio sounds that make sense (crying when sad, giggle when happy, success on completion)
- **YES** Animated virtual human interacting with the user (the girl reacts to every action with specific animations)
- **NO** AI integration
- **NO** Multi-user / collaborative system
- **YES** Script quality (well-structured, commented, etc.)
