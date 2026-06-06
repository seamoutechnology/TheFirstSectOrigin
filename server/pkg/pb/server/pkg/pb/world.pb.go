
package pb

import (
	protoreflect "google.golang.org/protobuf/reflect/protoreflect"
	protoimpl "google.golang.org/protobuf/runtime/protoimpl"
	reflect "reflect"
	unsafe "unsafe"
)

const (
	_ = protoimpl.EnforceVersion(20 - protoimpl.MinVersion)
	_ = protoimpl.EnforceVersion(protoimpl.MaxVersion - 20)
)

var File_world_proto protoreflect.FileDescriptor

const file_world_proto_rawDesc = "" +
	"\n" +
	"\vworld.proto\x12\x02pb\x1a\fcommon.proto\x1a\fplayer.proto\x1a\n" +
	"sect.proto\x1a\x0edisciple.proto\x1a\n" +
	"item.proto\x1a\vgacha.proto\x1a\rmission.proto\x1a\fsystem.proto2\x93\b\n" +
	"\x0eGatewayService\x12;\n" +
	"\n" +
	"GetVersion\x12\x15.pb.GetVersionRequest\x1a\x16.pb.GetVersionResponse\x122\n" +
	"\aGetBase\x12\x12.pb.GetBaseRequest\x1a\x13.pb.GetBaseResponse\x12A\n" +
	"\fCreatePlayer\x12\x17.pb.CreatePlayerRequest\x1a\x18.pb.CreatePlayerResponse\x12M\n" +
	"\x10GetPlayerProfile\x12\x1b.pb.GetPlayerProfileRequest\x1a\x1c.pb.GetPlayerProfileResponse\x122\n" +
	"\vGetSectInfo\x12\x15.pb.GetProfileRequest\x1a\f.pb.SectInfo\x12J\n" +
	"\x0fUpgradeBuilding\x12\x1a.pb.UpgradeBuildingRequest\x1a\x1b.pb.UpgradeBuildingResponse\x12M\n" +
	"\x10CollectResources\x12\x1b.pb.CollectResourcesRequest\x1a\x1c.pb.CollectResourcesResponse\x128\n" +
	"\tGetHeroes\x12\x14.pb.GetHeroesRequest\x1a\x15.pb.GetHeroesResponse\x12A\n" +
	"\fSetFormation\x12\x17.pb.SetFormationRequest\x1a\x18.pb.SetFormationResponse\x12J\n" +
	"\x0fLevelUpDisciple\x12\x1a.pb.LevelUpDiscipleRequest\x1a\x1b.pb.LevelUpDiscipleResponse\x12>\n" +
	"\vLevelUpHero\x12\x16.pb.LevelUpHeroRequest\x1a\x17.pb.LevelUpHeroResponse\x124\n" +
	"\fGetInventory\x12\x15.pb.GetProfileRequest\x1a\r.pb.Inventory\x120\n" +
	"\tEquipItem\x12\x10.pb.EquipRequest\x1a\x11.pb.EquipResponse\x12J\n" +
	"\x0fGetGachaBanners\x12\x1a.pb.GetGachaBannersRequest\x1a\x1b.pb.GetGachaBannersResponse\x122\n" +
	"\aDoGacha\x12\x12.pb.DoGachaRequest\x1a\x13.pb.DoGachaResponse\x12>\n" +
	"\vGetMissions\x12\x16.pb.GetMissionsRequest\x1a\x17.pb.GetMissionsResponse2\xf4\x01\n" +
	"\fWorldService\x122\n" +
	"\x10InternalSyncSect\x12\f.pb.SectInfo\x1a\x10.pb.BaseResponse\x12<\n" +
	"\rListCutscenes\x12\x10.pb.EmptyRequest\x1a\x19.pb.ListCutscenesResponse\x127\n" +
	"\vGetCutscene\x12\x16.pb.GetCutsceneRequest\x1a\x10.pb.CutsceneData\x129\n" +
	"\fSaveCutscene\x12\x17.pb.SaveCutsceneRequest\x1a\x10.pb.BaseResponseB*Z\x10server/pkg/pb;pb\xaa\x02\x15GameClient.Network.Pbb\x06proto3"

var file_world_proto_goTypes = []any{
	(*GetVersionRequest)(nil),        // 0: pb.GetVersionRequest
	(*GetBaseRequest)(nil),           // 1: pb.GetBaseRequest
	(*CreatePlayerRequest)(nil),      // 2: pb.CreatePlayerRequest
	(*GetPlayerProfileRequest)(nil),  // 3: pb.GetPlayerProfileRequest
	(*GetProfileRequest)(nil),        // 4: pb.GetProfileRequest
	(*UpgradeBuildingRequest)(nil),   // 5: pb.UpgradeBuildingRequest
	(*CollectResourcesRequest)(nil),  // 6: pb.CollectResourcesRequest
	(*GetHeroesRequest)(nil),         // 7: pb.GetHeroesRequest
	(*SetFormationRequest)(nil),      // 8: pb.SetFormationRequest
	(*LevelUpDiscipleRequest)(nil),   // 9: pb.LevelUpDiscipleRequest
	(*LevelUpHeroRequest)(nil),       // 10: pb.LevelUpHeroRequest
	(*EquipRequest)(nil),             // 11: pb.EquipRequest
	(*GetGachaBannersRequest)(nil),   // 12: pb.GetGachaBannersRequest
	(*DoGachaRequest)(nil),           // 13: pb.DoGachaRequest
	(*GetMissionsRequest)(nil),       // 14: pb.GetMissionsRequest
	(*SectInfo)(nil),                 // 15: pb.SectInfo
	(*EmptyRequest)(nil),             // 16: pb.EmptyRequest
	(*GetCutsceneRequest)(nil),       // 17: pb.GetCutsceneRequest
	(*SaveCutsceneRequest)(nil),      // 18: pb.SaveCutsceneRequest
	(*GetVersionResponse)(nil),       // 19: pb.GetVersionResponse
	(*GetBaseResponse)(nil),          // 20: pb.GetBaseResponse
	(*CreatePlayerResponse)(nil),     // 21: pb.CreatePlayerResponse
	(*GetPlayerProfileResponse)(nil), // 22: pb.GetPlayerProfileResponse
	(*UpgradeBuildingResponse)(nil),  // 23: pb.UpgradeBuildingResponse
	(*CollectResourcesResponse)(nil), // 24: pb.CollectResourcesResponse
	(*GetHeroesResponse)(nil),        // 25: pb.GetHeroesResponse
	(*SetFormationResponse)(nil),     // 26: pb.SetFormationResponse
	(*LevelUpDiscipleResponse)(nil),  // 27: pb.LevelUpDiscipleResponse
	(*LevelUpHeroResponse)(nil),      // 28: pb.LevelUpHeroResponse
	(*Inventory)(nil),                // 29: pb.Inventory
	(*EquipResponse)(nil),            // 30: pb.EquipResponse
	(*GetGachaBannersResponse)(nil),  // 31: pb.GetGachaBannersResponse
	(*DoGachaResponse)(nil),          // 32: pb.DoGachaResponse
	(*GetMissionsResponse)(nil),      // 33: pb.GetMissionsResponse
	(*BaseResponse)(nil),             // 34: pb.BaseResponse
	(*ListCutscenesResponse)(nil),    // 35: pb.ListCutscenesResponse
	(*CutsceneData)(nil),             // 36: pb.CutsceneData
}
var file_world_proto_depIdxs = []int32{
	0,  // 0: pb.GatewayService.GetVersion:input_type -> pb.GetVersionRequest
	1,  // 1: pb.GatewayService.GetBase:input_type -> pb.GetBaseRequest
	2,  // 2: pb.GatewayService.CreatePlayer:input_type -> pb.CreatePlayerRequest
	3,  // 3: pb.GatewayService.GetPlayerProfile:input_type -> pb.GetPlayerProfileRequest
	4,  // 4: pb.GatewayService.GetSectInfo:input_type -> pb.GetProfileRequest
	5,  // 5: pb.GatewayService.UpgradeBuilding:input_type -> pb.UpgradeBuildingRequest
	6,  // 6: pb.GatewayService.CollectResources:input_type -> pb.CollectResourcesRequest
	7,  // 7: pb.GatewayService.GetHeroes:input_type -> pb.GetHeroesRequest
	8,  // 8: pb.GatewayService.SetFormation:input_type -> pb.SetFormationRequest
	9,  // 9: pb.GatewayService.LevelUpDisciple:input_type -> pb.LevelUpDiscipleRequest
	10, // 10: pb.GatewayService.LevelUpHero:input_type -> pb.LevelUpHeroRequest
	4,  // 11: pb.GatewayService.GetInventory:input_type -> pb.GetProfileRequest
	11, // 12: pb.GatewayService.EquipItem:input_type -> pb.EquipRequest
	12, // 13: pb.GatewayService.GetGachaBanners:input_type -> pb.GetGachaBannersRequest
	13, // 14: pb.GatewayService.DoGacha:input_type -> pb.DoGachaRequest
	14, // 15: pb.GatewayService.GetMissions:input_type -> pb.GetMissionsRequest
	15, // 16: pb.WorldService.InternalSyncSect:input_type -> pb.SectInfo
	16, // 17: pb.WorldService.ListCutscenes:input_type -> pb.EmptyRequest
	17, // 18: pb.WorldService.GetCutscene:input_type -> pb.GetCutsceneRequest
	18, // 19: pb.WorldService.SaveCutscene:input_type -> pb.SaveCutsceneRequest
	19, // 20: pb.GatewayService.GetVersion:output_type -> pb.GetVersionResponse
	20, // 21: pb.GatewayService.GetBase:output_type -> pb.GetBaseResponse
	21, // 22: pb.GatewayService.CreatePlayer:output_type -> pb.CreatePlayerResponse
	22, // 23: pb.GatewayService.GetPlayerProfile:output_type -> pb.GetPlayerProfileResponse
	15, // 24: pb.GatewayService.GetSectInfo:output_type -> pb.SectInfo
	23, // 25: pb.GatewayService.UpgradeBuilding:output_type -> pb.UpgradeBuildingResponse
	24, // 26: pb.GatewayService.CollectResources:output_type -> pb.CollectResourcesResponse
	25, // 27: pb.GatewayService.GetHeroes:output_type -> pb.GetHeroesResponse
	26, // 28: pb.GatewayService.SetFormation:output_type -> pb.SetFormationResponse
	27, // 29: pb.GatewayService.LevelUpDisciple:output_type -> pb.LevelUpDiscipleResponse
	28, // 30: pb.GatewayService.LevelUpHero:output_type -> pb.LevelUpHeroResponse
	29, // 31: pb.GatewayService.GetInventory:output_type -> pb.Inventory
	30, // 32: pb.GatewayService.EquipItem:output_type -> pb.EquipResponse
	31, // 33: pb.GatewayService.GetGachaBanners:output_type -> pb.GetGachaBannersResponse
	32, // 34: pb.GatewayService.DoGacha:output_type -> pb.DoGachaResponse
	33, // 35: pb.GatewayService.GetMissions:output_type -> pb.GetMissionsResponse
	34, // 36: pb.WorldService.InternalSyncSect:output_type -> pb.BaseResponse
	35, // 37: pb.WorldService.ListCutscenes:output_type -> pb.ListCutscenesResponse
	36, // 38: pb.WorldService.GetCutscene:output_type -> pb.CutsceneData
	34, // 39: pb.WorldService.SaveCutscene:output_type -> pb.BaseResponse
	20, // [20:40] is the sub-list for method output_type
	0,  // [0:20] is the sub-list for method input_type
	0,  // [0:0] is the sub-list for extension type_name
	0,  // [0:0] is the sub-list for extension extendee
	0,  // [0:0] is the sub-list for field type_name
}

func init() { file_world_proto_init() }
func file_world_proto_init() {
	if File_world_proto != nil {
		return
	}
	file_common_proto_init()
	file_player_proto_init()
	file_sect_proto_init()
	file_disciple_proto_init()
	file_item_proto_init()
	file_gacha_proto_init()
	file_mission_proto_init()
	file_system_proto_init()
	type x struct{}
	out := protoimpl.TypeBuilder{
		File: protoimpl.DescBuilder{
			GoPackagePath: reflect.TypeOf(x{}).PkgPath(),
			RawDescriptor: unsafe.Slice(unsafe.StringData(file_world_proto_rawDesc), len(file_world_proto_rawDesc)),
			NumEnums:      0,
			NumMessages:   0,
			NumExtensions: 0,
			NumServices:   2,
		},
		GoTypes:           file_world_proto_goTypes,
		DependencyIndexes: file_world_proto_depIdxs,
	}.Build()
	File_world_proto = out.File
	file_world_proto_goTypes = nil
	file_world_proto_depIdxs = nil
}
