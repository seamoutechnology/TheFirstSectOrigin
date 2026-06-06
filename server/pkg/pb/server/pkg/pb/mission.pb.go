
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

type MissionType int32

const (
	MissionType_DAILY MissionType = 0
	MissionType_MAIN  MissionType = 1
	MissionType_SIDE  MissionType = 2
	MissionType_SECT  MissionType = 3 // Nhiệm vụ tông môn
)

var (
	MissionType_name = map[int32]string{
		0: "DAILY",
		1: "MAIN",
		2: "SIDE",
		3: "SECT",
	}
	MissionType_value = map[string]int32{
		"DAILY": 0,
		"MAIN":  1,
		"SIDE":  2,
		"SECT":  3,
	}
)

func (x MissionType) Enum() *MissionType {
	p := new(MissionType)
	*p = x
	return p
}

func (x MissionType) String() string {
	return protoimpl.X.EnumStringOf(x.Descriptor(), protoreflect.EnumNumber(x))
}

func (MissionType) Descriptor() protoreflect.EnumDescriptor {
	return file_mission_proto_enumTypes[0].Descriptor()
}

func (MissionType) Type() protoreflect.EnumType {
	return &file_mission_proto_enumTypes[0]
}

func (x MissionType) Number() protoreflect.EnumNumber {
	return protoreflect.EnumNumber(x)
}

func (MissionType) EnumDescriptor() ([]byte, []int) {
	return file_mission_proto_rawDescGZIP(), []int{0}
}

type MissionStatus int32

const (
	MissionStatus_LOCKED      MissionStatus = 0
	MissionStatus_AVAILABLE   MissionStatus = 1
	MissionStatus_IN_PROGRESS MissionStatus = 2
	MissionStatus_COMPLETED   MissionStatus = 3
	MissionStatus_REWARDED    MissionStatus = 4
)

var (
	MissionStatus_name = map[int32]string{
		0: "LOCKED",
		1: "AVAILABLE",
		2: "IN_PROGRESS",
		3: "COMPLETED",
		4: "REWARDED",
	}
	MissionStatus_value = map[string]int32{
		"LOCKED":      0,
		"AVAILABLE":   1,
		"IN_PROGRESS": 2,
		"COMPLETED":   3,
		"REWARDED":    4,
	}
)

func (x MissionStatus) Enum() *MissionStatus {
	p := new(MissionStatus)
	*p = x
	return p
}

func (x MissionStatus) String() string {
	return protoimpl.X.EnumStringOf(x.Descriptor(), protoreflect.EnumNumber(x))
}

func (MissionStatus) Descriptor() protoreflect.EnumDescriptor {
	return file_mission_proto_enumTypes[1].Descriptor()
}

func (MissionStatus) Type() protoreflect.EnumType {
	return &file_mission_proto_enumTypes[1]
}

func (x MissionStatus) Number() protoreflect.EnumNumber {
	return protoreflect.EnumNumber(x)
}

func (MissionStatus) EnumDescriptor() ([]byte, []int) {
	return file_mission_proto_rawDescGZIP(), []int{1}
}

type Mission struct {
	state           protoimpl.MessageState `protogen:"open.v1"`
	MissionId       int32                  `protobuf:"varint,1,opt,name=mission_id,json=missionId,proto3" json:"mission_id,omitempty"`
	Title           string                 `protobuf:"bytes,2,opt,name=title,proto3" json:"title,omitempty"`
	Description     string                 `protobuf:"bytes,3,opt,name=description,proto3" json:"description,omitempty"`
	Type            MissionType            `protobuf:"varint,4,opt,name=type,proto3,enum=pb.MissionType" json:"type,omitempty"`
	Status          MissionStatus          `protobuf:"varint,5,opt,name=status,proto3,enum=pb.MissionStatus" json:"status,omitempty"`
	CurrentProgress int32                  `protobuf:"varint,6,opt,name=current_progress,json=currentProgress,proto3" json:"current_progress,omitempty"`
	TargetProgress  int32                  `protobuf:"varint,7,opt,name=target_progress,json=targetProgress,proto3" json:"target_progress,omitempty"`
	Rewards         map[string]int32       `protobuf:"bytes,8,rep,name=rewards,proto3" json:"rewards,omitempty" protobuf_key:"bytes,1,opt,name=key" protobuf_val:"varint,2,opt,name=value"` // item_code -> quantity
	unknownFields   protoimpl.UnknownFields
	sizeCache       protoimpl.SizeCache
}

func (x *Mission) Reset() {
	*x = Mission{}
	mi := &file_mission_proto_msgTypes[0]
	ms := protoimpl.X.MessageStateOf(protoimpl.Pointer(x))
	ms.StoreMessageInfo(mi)
}

func (x *Mission) String() string {
	return protoimpl.X.MessageStringOf(x)
}

func (*Mission) ProtoMessage() {}

func (x *Mission) ProtoReflect() protoreflect.Message {
	mi := &file_mission_proto_msgTypes[0]
	if x != nil {
		ms := protoimpl.X.MessageStateOf(protoimpl.Pointer(x))
		if ms.LoadMessageInfo() == nil {
			ms.StoreMessageInfo(mi)
		}
		return ms
	}
	return mi.MessageOf(x)
}

func (*Mission) Descriptor() ([]byte, []int) {
	return file_mission_proto_rawDescGZIP(), []int{0}
}

func (x *Mission) GetMissionId() int32 {
	if x != nil {
		return x.MissionId
	}
	return 0
}

func (x *Mission) GetTitle() string {
	if x != nil {
		return x.Title
	}
	return ""
}

func (x *Mission) GetDescription() string {
	if x != nil {
		return x.Description
	}
	return ""
}

func (x *Mission) GetType() MissionType {
	if x != nil {
		return x.Type
	}
	return MissionType_DAILY
}

func (x *Mission) GetStatus() MissionStatus {
	if x != nil {
		return x.Status
	}
	return MissionStatus_LOCKED
}

func (x *Mission) GetCurrentProgress() int32 {
	if x != nil {
		return x.CurrentProgress
	}
	return 0
}

func (x *Mission) GetTargetProgress() int32 {
	if x != nil {
		return x.TargetProgress
	}
	return 0
}

func (x *Mission) GetRewards() map[string]int32 {
	if x != nil {
		return x.Rewards
	}
	return nil
}

type GetMissionsRequest struct {
	state         protoimpl.MessageState `protogen:"open.v1"`
	FilterType    MissionType            `protobuf:"varint,1,opt,name=filter_type,json=filterType,proto3,enum=pb.MissionType" json:"filter_type,omitempty"`
	unknownFields protoimpl.UnknownFields
	sizeCache     protoimpl.SizeCache
}

func (x *GetMissionsRequest) Reset() {
	*x = GetMissionsRequest{}
	mi := &file_mission_proto_msgTypes[1]
	ms := protoimpl.X.MessageStateOf(protoimpl.Pointer(x))
	ms.StoreMessageInfo(mi)
}

func (x *GetMissionsRequest) String() string {
	return protoimpl.X.MessageStringOf(x)
}

func (*GetMissionsRequest) ProtoMessage() {}

func (x *GetMissionsRequest) ProtoReflect() protoreflect.Message {
	mi := &file_mission_proto_msgTypes[1]
	if x != nil {
		ms := protoimpl.X.MessageStateOf(protoimpl.Pointer(x))
		if ms.LoadMessageInfo() == nil {
			ms.StoreMessageInfo(mi)
		}
		return ms
	}
	return mi.MessageOf(x)
}

func (*GetMissionsRequest) Descriptor() ([]byte, []int) {
	return file_mission_proto_rawDescGZIP(), []int{1}
}

func (x *GetMissionsRequest) GetFilterType() MissionType {
	if x != nil {
		return x.FilterType
	}
	return MissionType_DAILY
}

type GetMissionsResponse struct {
	state         protoimpl.MessageState `protogen:"open.v1"`
	Code          int32                  `protobuf:"varint,1,opt,name=code,proto3" json:"code,omitempty"`
	MessageId     string                 `protobuf:"bytes,2,opt,name=message_id,json=messageId,proto3" json:"message_id,omitempty"`
	Missions      []*Mission             `protobuf:"bytes,3,rep,name=missions,proto3" json:"missions,omitempty"`
	unknownFields protoimpl.UnknownFields
	sizeCache     protoimpl.SizeCache
}

func (x *GetMissionsResponse) Reset() {
	*x = GetMissionsResponse{}
	mi := &file_mission_proto_msgTypes[2]
	ms := protoimpl.X.MessageStateOf(protoimpl.Pointer(x))
	ms.StoreMessageInfo(mi)
}

func (x *GetMissionsResponse) String() string {
	return protoimpl.X.MessageStringOf(x)
}

func (*GetMissionsResponse) ProtoMessage() {}

func (x *GetMissionsResponse) ProtoReflect() protoreflect.Message {
	mi := &file_mission_proto_msgTypes[2]
	if x != nil {
		ms := protoimpl.X.MessageStateOf(protoimpl.Pointer(x))
		if ms.LoadMessageInfo() == nil {
			ms.StoreMessageInfo(mi)
		}
		return ms
	}
	return mi.MessageOf(x)
}

func (*GetMissionsResponse) Descriptor() ([]byte, []int) {
	return file_mission_proto_rawDescGZIP(), []int{2}
}

func (x *GetMissionsResponse) GetCode() int32 {
	if x != nil {
		return x.Code
	}
	return 0
}

func (x *GetMissionsResponse) GetMessageId() string {
	if x != nil {
		return x.MessageId
	}
	return ""
}

func (x *GetMissionsResponse) GetMissions() []*Mission {
	if x != nil {
		return x.Missions
	}
	return nil
}

type CompleteMissionRequest struct {
	state         protoimpl.MessageState `protogen:"open.v1"`
	MissionId     int32                  `protobuf:"varint,1,opt,name=mission_id,json=missionId,proto3" json:"mission_id,omitempty"`
	unknownFields protoimpl.UnknownFields
	sizeCache     protoimpl.SizeCache
}

func (x *CompleteMissionRequest) Reset() {
	*x = CompleteMissionRequest{}
	mi := &file_mission_proto_msgTypes[3]
	ms := protoimpl.X.MessageStateOf(protoimpl.Pointer(x))
	ms.StoreMessageInfo(mi)
}

func (x *CompleteMissionRequest) String() string {
	return protoimpl.X.MessageStringOf(x)
}

func (*CompleteMissionRequest) ProtoMessage() {}

func (x *CompleteMissionRequest) ProtoReflect() protoreflect.Message {
	mi := &file_mission_proto_msgTypes[3]
	if x != nil {
		ms := protoimpl.X.MessageStateOf(protoimpl.Pointer(x))
		if ms.LoadMessageInfo() == nil {
			ms.StoreMessageInfo(mi)
		}
		return ms
	}
	return mi.MessageOf(x)
}

func (*CompleteMissionRequest) Descriptor() ([]byte, []int) {
	return file_mission_proto_rawDescGZIP(), []int{3}
}

func (x *CompleteMissionRequest) GetMissionId() int32 {
	if x != nil {
		return x.MissionId
	}
	return 0
}

type CompleteMissionResponse struct {
	state         protoimpl.MessageState `protogen:"open.v1"`
	Code          int32                  `protobuf:"varint,1,opt,name=code,proto3" json:"code,omitempty"`
	MessageId     string                 `protobuf:"bytes,2,opt,name=message_id,json=messageId,proto3" json:"message_id,omitempty"`
	Rewards       map[string]int32       `protobuf:"bytes,3,rep,name=rewards,proto3" json:"rewards,omitempty" protobuf_key:"bytes,1,opt,name=key" protobuf_val:"varint,2,opt,name=value"`
	unknownFields protoimpl.UnknownFields
	sizeCache     protoimpl.SizeCache
}

func (x *CompleteMissionResponse) Reset() {
	*x = CompleteMissionResponse{}
	mi := &file_mission_proto_msgTypes[4]
	ms := protoimpl.X.MessageStateOf(protoimpl.Pointer(x))
	ms.StoreMessageInfo(mi)
}

func (x *CompleteMissionResponse) String() string {
	return protoimpl.X.MessageStringOf(x)
}

func (*CompleteMissionResponse) ProtoMessage() {}

func (x *CompleteMissionResponse) ProtoReflect() protoreflect.Message {
	mi := &file_mission_proto_msgTypes[4]
	if x != nil {
		ms := protoimpl.X.MessageStateOf(protoimpl.Pointer(x))
		if ms.LoadMessageInfo() == nil {
			ms.StoreMessageInfo(mi)
		}
		return ms
	}
	return mi.MessageOf(x)
}

func (*CompleteMissionResponse) Descriptor() ([]byte, []int) {
	return file_mission_proto_rawDescGZIP(), []int{4}
}

func (x *CompleteMissionResponse) GetCode() int32 {
	if x != nil {
		return x.Code
	}
	return 0
}

func (x *CompleteMissionResponse) GetMessageId() string {
	if x != nil {
		return x.MessageId
	}
	return ""
}

func (x *CompleteMissionResponse) GetRewards() map[string]int32 {
	if x != nil {
		return x.Rewards
	}
	return nil
}

var File_mission_proto protoreflect.FileDescriptor

const file_mission_proto_rawDesc = "" +
	"\n" +
	"\rmission.proto\x12\x02pb\x1a\fcommon.proto\"\xf4\x02\n" +
	"\aMission\x12\x1d\n" +
	"\n" +
	"mission_id\x18\x01 \x01(\x05R\tmissionId\x12\x14\n" +
	"\x05title\x18\x02 \x01(\tR\x05title\x12 \n" +
	"\vdescription\x18\x03 \x01(\tR\vdescription\x12#\n" +
	"\x04type\x18\x04 \x01(\x0e2\x0f.pb.MissionTypeR\x04type\x12)\n" +
	"\x06status\x18\x05 \x01(\x0e2\x11.pb.MissionStatusR\x06status\x12)\n" +
	"\x10current_progress\x18\x06 \x01(\x05R\x0fcurrentProgress\x12'\n" +
	"\x0ftarget_progress\x18\a \x01(\x05R\x0etargetProgress\x122\n" +
	"\arewards\x18\b \x03(\v2\x18.pb.Mission.RewardsEntryR\arewards\x1a:\n" +
	"\fRewardsEntry\x12\x10\n" +
	"\x03key\x18\x01 \x01(\tR\x03key\x12\x14\n" +
	"\x05value\x18\x02 \x01(\x05R\x05value:\x028\x01\"F\n" +
	"\x12GetMissionsRequest\x120\n" +
	"\vfilter_type\x18\x01 \x01(\x0e2\x0f.pb.MissionTypeR\n" +
	"filterType\"q\n" +
	"\x13GetMissionsResponse\x12\x12\n" +
	"\x04code\x18\x01 \x01(\x05R\x04code\x12\x1d\n" +
	"\n" +
	"message_id\x18\x02 \x01(\tR\tmessageId\x12'\n" +
	"\bmissions\x18\x03 \x03(\v2\v.pb.MissionR\bmissions\"7\n" +
	"\x16CompleteMissionRequest\x12\x1d\n" +
	"\n" +
	"mission_id\x18\x01 \x01(\x05R\tmissionId\"\xcc\x01\n" +
	"\x17CompleteMissionResponse\x12\x12\n" +
	"\x04code\x18\x01 \x01(\x05R\x04code\x12\x1d\n" +
	"\n" +
	"message_id\x18\x02 \x01(\tR\tmessageId\x12B\n" +
	"\arewards\x18\x03 \x03(\v2(.pb.CompleteMissionResponse.RewardsEntryR\arewards\x1a:\n" +
	"\fRewardsEntry\x12\x10\n" +
	"\x03key\x18\x01 \x01(\tR\x03key\x12\x14\n" +
	"\x05value\x18\x02 \x01(\x05R\x05value:\x028\x01*6\n" +
	"\vMissionType\x12\t\n" +
	"\x05DAILY\x10\x00\x12\b\n" +
	"\x04MAIN\x10\x01\x12\b\n" +
	"\x04SIDE\x10\x02\x12\b\n" +
	"\x04SECT\x10\x03*X\n" +
	"\rMissionStatus\x12\n" +
	"\n" +
	"\x06LOCKED\x10\x00\x12\r\n" +
	"\tAVAILABLE\x10\x01\x12\x0f\n" +
	"\vIN_PROGRESS\x10\x02\x12\r\n" +
	"\tCOMPLETED\x10\x03\x12\f\n" +
	"\bREWARDED\x10\x04B*Z\x10server/pkg/pb;pb\xaa\x02\x15GameClient.Network.Pbb\x06proto3"

var (
	file_mission_proto_rawDescOnce sync.Once
	file_mission_proto_rawDescData []byte
)

func file_mission_proto_rawDescGZIP() []byte {
	file_mission_proto_rawDescOnce.Do(func() {
		file_mission_proto_rawDescData = protoimpl.X.CompressGZIP(unsafe.Slice(unsafe.StringData(file_mission_proto_rawDesc), len(file_mission_proto_rawDesc)))
	})
	return file_mission_proto_rawDescData
}

var file_mission_proto_enumTypes = make([]protoimpl.EnumInfo, 2)
var file_mission_proto_msgTypes = make([]protoimpl.MessageInfo, 7)
var file_mission_proto_goTypes = []any{
	(MissionType)(0),                // 0: pb.MissionType
	(MissionStatus)(0),              // 1: pb.MissionStatus
	(*Mission)(nil),                 // 2: pb.Mission
	(*GetMissionsRequest)(nil),      // 3: pb.GetMissionsRequest
	(*GetMissionsResponse)(nil),     // 4: pb.GetMissionsResponse
	(*CompleteMissionRequest)(nil),  // 5: pb.CompleteMissionRequest
	(*CompleteMissionResponse)(nil), // 6: pb.CompleteMissionResponse
	nil,                             // 7: pb.Mission.RewardsEntry
	nil,                             // 8: pb.CompleteMissionResponse.RewardsEntry
}
var file_mission_proto_depIdxs = []int32{
	0, // 0: pb.Mission.type:type_name -> pb.MissionType
	1, // 1: pb.Mission.status:type_name -> pb.MissionStatus
	7, // 2: pb.Mission.rewards:type_name -> pb.Mission.RewardsEntry
	0, // 3: pb.GetMissionsRequest.filter_type:type_name -> pb.MissionType
	2, // 4: pb.GetMissionsResponse.missions:type_name -> pb.Mission
	8, // 5: pb.CompleteMissionResponse.rewards:type_name -> pb.CompleteMissionResponse.RewardsEntry
	6, // [6:6] is the sub-list for method output_type
	6, // [6:6] is the sub-list for method input_type
	6, // [6:6] is the sub-list for extension type_name
	6, // [6:6] is the sub-list for extension extendee
	0, // [0:6] is the sub-list for field type_name
}

func init() { file_mission_proto_init() }
func file_mission_proto_init() {
	if File_mission_proto != nil {
		return
	}
	file_common_proto_init()
	type x struct{}
	out := protoimpl.TypeBuilder{
		File: protoimpl.DescBuilder{
			GoPackagePath: reflect.TypeOf(x{}).PkgPath(),
			RawDescriptor: unsafe.Slice(unsafe.StringData(file_mission_proto_rawDesc), len(file_mission_proto_rawDesc)),
			NumEnums:      2,
			NumMessages:   7,
			NumExtensions: 0,
			NumServices:   0,
		},
		GoTypes:           file_mission_proto_goTypes,
		DependencyIndexes: file_mission_proto_depIdxs,
		EnumInfos:         file_mission_proto_enumTypes,
		MessageInfos:      file_mission_proto_msgTypes,
	}.Build()
	File_mission_proto = out.File
	file_mission_proto_goTypes = nil
	file_mission_proto_depIdxs = nil
}
