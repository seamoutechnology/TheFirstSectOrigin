
package pb

import (
	protoreflect "google.golang.org/protobuf/reflect/protoreflect"
	protoimpl "google.golang.org/protobuf/runtime/protoimpl"
	reflect "reflect"
	sync "sync"
	unsafe "unsafe"
)

const (
	_ = protoimpl.EnforceVersion(20 - protoimpl.MinVersion)
	_ = protoimpl.EnforceVersion(protoimpl.MaxVersion - 20)
)

type Building struct {
	state         protoimpl.MessageState `protogen:"open.v1"`
	BuildingCode  string                 `protobuf:"bytes,1,opt,name=building_code,json=buildingCode,proto3" json:"building_code,omitempty"`
	Name          string                 `protobuf:"bytes,2,opt,name=name,proto3" json:"name,omitempty"`
	Level         int32                  `protobuf:"varint,3,opt,name=level,proto3" json:"level,omitempty"`
	UpgradeEndAt  int64                  `protobuf:"varint,4,opt,name=upgrade_end_at,json=upgradeEndAt,proto3" json:"upgrade_end_at,omitempty"`
	LastCollectAt int64                  `protobuf:"varint,5,opt,name=last_collect_at,json=lastCollectAt,proto3" json:"last_collect_at,omitempty"`
	PendingGold   int64                  `protobuf:"varint,6,opt,name=pending_gold,json=pendingGold,proto3" json:"pending_gold,omitempty"`
	MaxLevel      int32                  `protobuf:"varint,7,opt,name=max_level,json=maxLevel,proto3" json:"max_level,omitempty"`
	unknownFields protoimpl.UnknownFields
	sizeCache     protoimpl.SizeCache
}

func (x *Building) Reset() {
	*x = Building{}
	mi := &file_sect_proto_msgTypes[0]
	ms := protoimpl.X.MessageStateOf(protoimpl.Pointer(x))
	ms.StoreMessageInfo(mi)
}

func (x *Building) String() string {
	return protoimpl.X.MessageStringOf(x)
}

func (*Building) ProtoMessage() {}

func (x *Building) ProtoReflect() protoreflect.Message {
	mi := &file_sect_proto_msgTypes[0]
	if x != nil {
		ms := protoimpl.X.MessageStateOf(protoimpl.Pointer(x))
		if ms.LoadMessageInfo() == nil {
			ms.StoreMessageInfo(mi)
		}
		return ms
	}
	return mi.MessageOf(x)
}

func (*Building) Descriptor() ([]byte, []int) {
	return file_sect_proto_rawDescGZIP(), []int{0}
}

func (x *Building) GetBuildingCode() string {
	if x != nil {
		return x.BuildingCode
	}
	return ""
}

func (x *Building) GetName() string {
	if x != nil {
		return x.Name
	}
	return ""
}

func (x *Building) GetLevel() int32 {
	if x != nil {
		return x.Level
	}
	return 0
}

func (x *Building) GetUpgradeEndAt() int64 {
	if x != nil {
		return x.UpgradeEndAt
	}
	return 0
}

func (x *Building) GetLastCollectAt() int64 {
	if x != nil {
		return x.LastCollectAt
	}
	return 0
}

func (x *Building) GetPendingGold() int64 {
	if x != nil {
		return x.PendingGold
	}
	return 0
}

func (x *Building) GetMaxLevel() int32 {
	if x != nil {
		return x.MaxLevel
	}
	return 0
}

type AlignmentInfo struct {
	state            protoimpl.MessageState `protogen:"open.v1"`
	CurrentAlignment int32                  `protobuf:"varint,1,opt,name=current_alignment,json=currentAlignment,proto3" json:"current_alignment,omitempty"`
	KarmaPoints      int32                  `protobuf:"varint,2,opt,name=karma_points,json=karmaPoints,proto3" json:"karma_points,omitempty"`
	Title            string                 `protobuf:"bytes,3,opt,name=title,proto3" json:"title,omitempty"`
	unknownFields    protoimpl.UnknownFields
	sizeCache        protoimpl.SizeCache
}

func (x *AlignmentInfo) Reset() {
	*x = AlignmentInfo{}
	mi := &file_sect_proto_msgTypes[1]
	ms := protoimpl.X.MessageStateOf(protoimpl.Pointer(x))
	ms.StoreMessageInfo(mi)
}

func (x *AlignmentInfo) String() string {
	return protoimpl.X.MessageStringOf(x)
}

func (*AlignmentInfo) ProtoMessage() {}

func (x *AlignmentInfo) ProtoReflect() protoreflect.Message {
	mi := &file_sect_proto_msgTypes[1]
	if x != nil {
		ms := protoimpl.X.MessageStateOf(protoimpl.Pointer(x))
		if ms.LoadMessageInfo() == nil {
			ms.StoreMessageInfo(mi)
		}
		return ms
	}
	return mi.MessageOf(x)
}

func (*AlignmentInfo) Descriptor() ([]byte, []int) {
	return file_sect_proto_rawDescGZIP(), []int{1}
}

func (x *AlignmentInfo) GetCurrentAlignment() int32 {
	if x != nil {
		return x.CurrentAlignment
	}
	return 0
}

func (x *AlignmentInfo) GetKarmaPoints() int32 {
	if x != nil {
		return x.KarmaPoints
	}
	return 0
}

func (x *AlignmentInfo) GetTitle() string {
	if x != nil {
		return x.Title
	}
	return ""
}

type SectInfo struct {
	state         protoimpl.MessageState `protogen:"open.v1"`
	Base          *BaseResponse          `protobuf:"bytes,1,opt,name=base,proto3" json:"base,omitempty"`
	SectName      string                 `protobuf:"bytes,2,opt,name=sect_name,json=sectName,proto3" json:"sect_name,omitempty"`
	Buildings     []*Building            `protobuf:"bytes,3,rep,name=buildings,proto3" json:"buildings,omitempty"`
	Alignment     *AlignmentInfo         `protobuf:"bytes,4,opt,name=alignment,proto3" json:"alignment,omitempty"`
	unknownFields protoimpl.UnknownFields
	sizeCache     protoimpl.SizeCache
}

func (x *SectInfo) Reset() {
	*x = SectInfo{}
	mi := &file_sect_proto_msgTypes[2]
	ms := protoimpl.X.MessageStateOf(protoimpl.Pointer(x))
	ms.StoreMessageInfo(mi)
}

func (x *SectInfo) String() string {
	return protoimpl.X.MessageStringOf(x)
}

func (*SectInfo) ProtoMessage() {}

func (x *SectInfo) ProtoReflect() protoreflect.Message {
	mi := &file_sect_proto_msgTypes[2]
	if x != nil {
		ms := protoimpl.X.MessageStateOf(protoimpl.Pointer(x))
		if ms.LoadMessageInfo() == nil {
			ms.StoreMessageInfo(mi)
		}
		return ms
	}
	return mi.MessageOf(x)
}

func (*SectInfo) Descriptor() ([]byte, []int) {
	return file_sect_proto_rawDescGZIP(), []int{2}
}

func (x *SectInfo) GetBase() *BaseResponse {
	if x != nil {
		return x.Base
	}
	return nil
}

func (x *SectInfo) GetSectName() string {
	if x != nil {
		return x.SectName
	}
	return ""
}

func (x *SectInfo) GetBuildings() []*Building {
	if x != nil {
		return x.Buildings
	}
	return nil
}

func (x *SectInfo) GetAlignment() *AlignmentInfo {
	if x != nil {
		return x.Alignment
	}
	return nil
}

type UpgradeBuildingRequest struct {
	state         protoimpl.MessageState `protogen:"open.v1"`
	BuildingCode  string                 `protobuf:"bytes,1,opt,name=building_code,json=buildingCode,proto3" json:"building_code,omitempty"`
	unknownFields protoimpl.UnknownFields
	sizeCache     protoimpl.SizeCache
}

func (x *UpgradeBuildingRequest) Reset() {
	*x = UpgradeBuildingRequest{}
	mi := &file_sect_proto_msgTypes[3]
	ms := protoimpl.X.MessageStateOf(protoimpl.Pointer(x))
	ms.StoreMessageInfo(mi)
}

func (x *UpgradeBuildingRequest) String() string {
	return protoimpl.X.MessageStringOf(x)
}

func (*UpgradeBuildingRequest) ProtoMessage() {}

func (x *UpgradeBuildingRequest) ProtoReflect() protoreflect.Message {
	mi := &file_sect_proto_msgTypes[3]
	if x != nil {
		ms := protoimpl.X.MessageStateOf(protoimpl.Pointer(x))
		if ms.LoadMessageInfo() == nil {
			ms.StoreMessageInfo(mi)
		}
		return ms
	}
	return mi.MessageOf(x)
}

func (*UpgradeBuildingRequest) Descriptor() ([]byte, []int) {
	return file_sect_proto_rawDescGZIP(), []int{3}
}

func (x *UpgradeBuildingRequest) GetBuildingCode() string {
	if x != nil {
		return x.BuildingCode
	}
	return ""
}

type CollectResourcesRequest struct {
	state         protoimpl.MessageState `protogen:"open.v1"`
	BuildingCode  string                 `protobuf:"bytes,1,opt,name=building_code,json=buildingCode,proto3" json:"building_code,omitempty"` // Thêm mã công trình
	unknownFields protoimpl.UnknownFields
	sizeCache     protoimpl.SizeCache
}

func (x *CollectResourcesRequest) Reset() {
	*x = CollectResourcesRequest{}
	mi := &file_sect_proto_msgTypes[4]
	ms := protoimpl.X.MessageStateOf(protoimpl.Pointer(x))
	ms.StoreMessageInfo(mi)
}

func (x *CollectResourcesRequest) String() string {
	return protoimpl.X.MessageStringOf(x)
}

func (*CollectResourcesRequest) ProtoMessage() {}

func (x *CollectResourcesRequest) ProtoReflect() protoreflect.Message {
	mi := &file_sect_proto_msgTypes[4]
	if x != nil {
		ms := protoimpl.X.MessageStateOf(protoimpl.Pointer(x))
		if ms.LoadMessageInfo() == nil {
			ms.StoreMessageInfo(mi)
		}
		return ms
	}
	return mi.MessageOf(x)
}

func (*CollectResourcesRequest) Descriptor() ([]byte, []int) {
	return file_sect_proto_rawDescGZIP(), []int{4}
}

func (x *CollectResourcesRequest) GetBuildingCode() string {
	if x != nil {
		return x.BuildingCode
	}
	return ""
}

type GetBaseResponse struct {
	state         protoimpl.MessageState `protogen:"open.v1"`
	Base          *BaseResponse          `protobuf:"bytes,1,opt,name=base,proto3" json:"base,omitempty"`
	Buildings     []*Building            `protobuf:"bytes,2,rep,name=buildings,proto3" json:"buildings,omitempty"` // C# đang tìm .Buildings
	unknownFields protoimpl.UnknownFields
	sizeCache     protoimpl.SizeCache
}

func (x *GetBaseResponse) Reset() {
	*x = GetBaseResponse{}
	mi := &file_sect_proto_msgTypes[5]
	ms := protoimpl.X.MessageStateOf(protoimpl.Pointer(x))
	ms.StoreMessageInfo(mi)
}

func (x *GetBaseResponse) String() string {
	return protoimpl.X.MessageStringOf(x)
}

func (*GetBaseResponse) ProtoMessage() {}

func (x *GetBaseResponse) ProtoReflect() protoreflect.Message {
	mi := &file_sect_proto_msgTypes[5]
	if x != nil {
		ms := protoimpl.X.MessageStateOf(protoimpl.Pointer(x))
		if ms.LoadMessageInfo() == nil {
			ms.StoreMessageInfo(mi)
		}
		return ms
	}
	return mi.MessageOf(x)
}

func (*GetBaseResponse) Descriptor() ([]byte, []int) {
	return file_sect_proto_rawDescGZIP(), []int{5}
}

func (x *GetBaseResponse) GetBase() *BaseResponse {
	if x != nil {
		return x.Base
	}
	return nil
}

func (x *GetBaseResponse) GetBuildings() []*Building {
	if x != nil {
		return x.Buildings
	}
	return nil
}

type UpgradeBuildingResponse struct {
	state         protoimpl.MessageState `protogen:"open.v1"`
	Base          *BaseResponse          `protobuf:"bytes,1,opt,name=base,proto3" json:"base,omitempty"`
	Building      *Building              `protobuf:"bytes,2,opt,name=building,proto3" json:"building,omitempty"`
	unknownFields protoimpl.UnknownFields
	sizeCache     protoimpl.SizeCache
}

func (x *UpgradeBuildingResponse) Reset() {
	*x = UpgradeBuildingResponse{}
	mi := &file_sect_proto_msgTypes[6]
	ms := protoimpl.X.MessageStateOf(protoimpl.Pointer(x))
	ms.StoreMessageInfo(mi)
}

func (x *UpgradeBuildingResponse) String() string {
	return protoimpl.X.MessageStringOf(x)
}

func (*UpgradeBuildingResponse) ProtoMessage() {}

func (x *UpgradeBuildingResponse) ProtoReflect() protoreflect.Message {
	mi := &file_sect_proto_msgTypes[6]
	if x != nil {
		ms := protoimpl.X.MessageStateOf(protoimpl.Pointer(x))
		if ms.LoadMessageInfo() == nil {
			ms.StoreMessageInfo(mi)
		}
		return ms
	}
	return mi.MessageOf(x)
}

func (*UpgradeBuildingResponse) Descriptor() ([]byte, []int) {
	return file_sect_proto_rawDescGZIP(), []int{6}
}

func (x *UpgradeBuildingResponse) GetBase() *BaseResponse {
	if x != nil {
		return x.Base
	}
	return nil
}

func (x *UpgradeBuildingResponse) GetBuilding() *Building {
	if x != nil {
		return x.Building
	}
	return nil
}

type CollectResourcesResponse struct {
	state         protoimpl.MessageState `protogen:"open.v1"`
	Base          *BaseResponse          `protobuf:"bytes,1,opt,name=base,proto3" json:"base,omitempty"`
	GoldGained    int64                  `protobuf:"varint,2,opt,name=gold_gained,json=goldGained,proto3" json:"gold_gained,omitempty"`
	Player        *PlayerProfile         `protobuf:"bytes,3,opt,name=player,proto3" json:"player,omitempty"`
	Resources     map[string]int64       `protobuf:"bytes,4,rep,name=resources,proto3" json:"resources,omitempty" protobuf_key:"bytes,1,opt,name=key" protobuf_val:"varint,2,opt,name=value"`
	unknownFields protoimpl.UnknownFields
	sizeCache     protoimpl.SizeCache
}

func (x *CollectResourcesResponse) Reset() {
	*x = CollectResourcesResponse{}
	mi := &file_sect_proto_msgTypes[7]
	ms := protoimpl.X.MessageStateOf(protoimpl.Pointer(x))
	ms.StoreMessageInfo(mi)
}

func (x *CollectResourcesResponse) String() string {
	return protoimpl.X.MessageStringOf(x)
}

func (*CollectResourcesResponse) ProtoMessage() {}

func (x *CollectResourcesResponse) ProtoReflect() protoreflect.Message {
	mi := &file_sect_proto_msgTypes[7]
	if x != nil {
		ms := protoimpl.X.MessageStateOf(protoimpl.Pointer(x))
		if ms.LoadMessageInfo() == nil {
			ms.StoreMessageInfo(mi)
		}
		return ms
	}
	return mi.MessageOf(x)
}

func (*CollectResourcesResponse) Descriptor() ([]byte, []int) {
	return file_sect_proto_rawDescGZIP(), []int{7}
}

func (x *CollectResourcesResponse) GetBase() *BaseResponse {
	if x != nil {
		return x.Base
	}
	return nil
}

func (x *CollectResourcesResponse) GetGoldGained() int64 {
	if x != nil {
		return x.GoldGained
	}
	return 0
}

func (x *CollectResourcesResponse) GetPlayer() *PlayerProfile {
	if x != nil {
		return x.Player
	}
	return nil
}

func (x *CollectResourcesResponse) GetResources() map[string]int64 {
	if x != nil {
		return x.Resources
	}
	return nil
}

var File_sect_proto protoreflect.FileDescriptor

const file_sect_proto_rawDesc = "" +
	"\n" +
	"\n" +
	"sect.proto\x12\x02pb\x1a\fcommon.proto\x1a\fplayer.proto\"\xe7\x01\n" +
	"\bBuilding\x12#\n" +
	"\rbuilding_code\x18\x01 \x01(\tR\fbuildingCode\x12\x12\n" +
	"\x04name\x18\x02 \x01(\tR\x04name\x12\x14\n" +
	"\x05level\x18\x03 \x01(\x05R\x05level\x12$\n" +
	"\x0eupgrade_end_at\x18\x04 \x01(\x03R\fupgradeEndAt\x12&\n" +
	"\x0flast_collect_at\x18\x05 \x01(\x03R\rlastCollectAt\x12!\n" +
	"\fpending_gold\x18\x06 \x01(\x03R\vpendingGold\x12\x1b\n" +
	"\tmax_level\x18\a \x01(\x05R\bmaxLevel\"u\n" +
	"\rAlignmentInfo\x12+\n" +
	"\x11current_alignment\x18\x01 \x01(\x05R\x10currentAlignment\x12!\n" +
	"\fkarma_points\x18\x02 \x01(\x05R\vkarmaPoints\x12\x14\n" +
	"\x05title\x18\x03 \x01(\tR\x05title\"\xaa\x01\n" +
	"\bSectInfo\x12$\n" +
	"\x04base\x18\x01 \x01(\v2\x10.pb.BaseResponseR\x04base\x12\x1b\n" +
	"\tsect_name\x18\x02 \x01(\tR\bsectName\x12*\n" +
	"\tbuildings\x18\x03 \x03(\v2\f.pb.BuildingR\tbuildings\x12/\n" +
	"\talignment\x18\x04 \x01(\v2\x11.pb.AlignmentInfoR\talignment\"=\n" +
	"\x16UpgradeBuildingRequest\x12#\n" +
	"\rbuilding_code\x18\x01 \x01(\tR\fbuildingCode\">\n" +
	"\x17CollectResourcesRequest\x12#\n" +
	"\rbuilding_code\x18\x01 \x01(\tR\fbuildingCode\"c\n" +
	"\x0fGetBaseResponse\x12$\n" +
	"\x04base\x18\x01 \x01(\v2\x10.pb.BaseResponseR\x04base\x12*\n" +
	"\tbuildings\x18\x02 \x03(\v2\f.pb.BuildingR\tbuildings\"i\n" +
	"\x17UpgradeBuildingResponse\x12$\n" +
	"\x04base\x18\x01 \x01(\v2\x10.pb.BaseResponseR\x04base\x12(\n" +
	"\bbuilding\x18\x02 \x01(\v2\f.pb.BuildingR\bbuilding\"\x95\x02\n" +
	"\x18CollectResourcesResponse\x12$\n" +
	"\x04base\x18\x01 \x01(\v2\x10.pb.BaseResponseR\x04base\x12\x1f\n" +
	"\vgold_gained\x18\x02 \x01(\x03R\n" +
	"goldGained\x12)\n" +
	"\x06player\x18\x03 \x01(\v2\x11.pb.PlayerProfileR\x06player\x12I\n" +
	"\tresources\x18\x04 \x03(\v2+.pb.CollectResourcesResponse.ResourcesEntryR\tresources\x1a<\n" +
	"\x0eResourcesEntry\x12\x10\n" +
	"\x03key\x18\x01 \x01(\tR\x03key\x12\x14\n" +
	"\x05value\x18\x02 \x01(\x03R\x05value:\x028\x01B*Z\x10server/pkg/pb;pb\xaa\x02\x15GameClient.Network.Pbb\x06proto3"

var (
	file_sect_proto_rawDescOnce sync.Once
	file_sect_proto_rawDescData []byte
)

func file_sect_proto_rawDescGZIP() []byte {
	file_sect_proto_rawDescOnce.Do(func() {
		file_sect_proto_rawDescData = protoimpl.X.CompressGZIP(unsafe.Slice(unsafe.StringData(file_sect_proto_rawDesc), len(file_sect_proto_rawDesc)))
	})
	return file_sect_proto_rawDescData
}

var file_sect_proto_msgTypes = make([]protoimpl.MessageInfo, 9)
var file_sect_proto_goTypes = []any{
	(*Building)(nil),                 // 0: pb.Building
	(*AlignmentInfo)(nil),            // 1: pb.AlignmentInfo
	(*SectInfo)(nil),                 // 2: pb.SectInfo
	(*UpgradeBuildingRequest)(nil),   // 3: pb.UpgradeBuildingRequest
	(*CollectResourcesRequest)(nil),  // 4: pb.CollectResourcesRequest
	(*GetBaseResponse)(nil),          // 5: pb.GetBaseResponse
	(*UpgradeBuildingResponse)(nil),  // 6: pb.UpgradeBuildingResponse
	(*CollectResourcesResponse)(nil), // 7: pb.CollectResourcesResponse
	nil,                              // 8: pb.CollectResourcesResponse.ResourcesEntry
	(*BaseResponse)(nil),             // 9: pb.BaseResponse
	(*PlayerProfile)(nil),            // 10: pb.PlayerProfile
}
var file_sect_proto_depIdxs = []int32{
	9,  // 0: pb.SectInfo.base:type_name -> pb.BaseResponse
	0,  // 1: pb.SectInfo.buildings:type_name -> pb.Building
	1,  // 2: pb.SectInfo.alignment:type_name -> pb.AlignmentInfo
	9,  // 3: pb.GetBaseResponse.base:type_name -> pb.BaseResponse
	0,  // 4: pb.GetBaseResponse.buildings:type_name -> pb.Building
	9,  // 5: pb.UpgradeBuildingResponse.base:type_name -> pb.BaseResponse
	0,  // 6: pb.UpgradeBuildingResponse.building:type_name -> pb.Building
	9,  // 7: pb.CollectResourcesResponse.base:type_name -> pb.BaseResponse
	10, // 8: pb.CollectResourcesResponse.player:type_name -> pb.PlayerProfile
	8,  // 9: pb.CollectResourcesResponse.resources:type_name -> pb.CollectResourcesResponse.ResourcesEntry
	10, // [10:10] is the sub-list for method output_type
	10, // [10:10] is the sub-list for method input_type
	10, // [10:10] is the sub-list for extension type_name
	10, // [10:10] is the sub-list for extension extendee
	0,  // [0:10] is the sub-list for field type_name
}

func init() { file_sect_proto_init() }
func file_sect_proto_init() {
	if File_sect_proto != nil {
		return
	}
	file_common_proto_init()
	file_player_proto_init()
	type x struct{}
	out := protoimpl.TypeBuilder{
		File: protoimpl.DescBuilder{
			GoPackagePath: reflect.TypeOf(x{}).PkgPath(),
			RawDescriptor: unsafe.Slice(unsafe.StringData(file_sect_proto_rawDesc), len(file_sect_proto_rawDesc)),
			NumEnums:      0,
			NumMessages:   9,
			NumExtensions: 0,
			NumServices:   0,
		},
		GoTypes:           file_sect_proto_goTypes,
		DependencyIndexes: file_sect_proto_depIdxs,
		MessageInfos:      file_sect_proto_msgTypes,
	}.Build()
	File_sect_proto = out.File
	file_sect_proto_goTypes = nil
	file_sect_proto_depIdxs = nil
}
