# VMCBlendShapeControl

Beat SaberからVirtualMotionCaptureへ、曲時間およびゲームイベントに連動したBlendShape制御を送信するModです。

## Current implementation status

- Project scaffold compatible with BSIPA/SiraUtil/BSML
- Time-based expression trigger from JSON script
- Gameplay event-based trigger pipeline
- OSC sender for `/VMC/Ext/Blend/Val` and `/VMC/Ext/Blend/Apply`
- OSC receiver for BlendShape discovery from VMC outgoing packets
- Basic settings UI and discovered BlendShape preview

## Script file

Default path:

`UserData/VMCBlendShapeControl/DefaultVMCBlendShape.json`

Song-specific script file name:

`SongVMCBlendShape.json`

## Notes

This is an initial implementation start. Action detail editing UI and advanced conflict resolution are planned for subsequent phases.
