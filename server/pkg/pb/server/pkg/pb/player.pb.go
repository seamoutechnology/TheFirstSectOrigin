
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

type PlayerProfile struct {
	state         protoimpl.MessageState `protogen:"open.v1"`
	UserId        int64                  `protobuf:"varint,1,opt,name=user_id,json=userId,proto3" json:"user_id,omitempty"`
	Nickname      string                 `protobuf:"bytes,2,opt,name=nickname,proto3" json:"nickname,omitempty"`
	Level         int32                  `protobuf:"varint,3,opt,name=level,proto3" json:"level,omitempty"`
	Exp           int64                  `protobuf:"varint,4,opt,name=exp,proto3" json:"exp,omitempty"`
	Gold          int64                  `protobuf:"varint,5,opt,name=gold,proto3" json:"gold,omitempty"`
	Diamond       int64                  `protobuf:"varint,6,opt,name=diamond,proto3" json:"diamond,omitempty"`
	Stamina       int32                  `protobuf:"varint,7,opt,name=stamina,proto3" json:"stamina,omitempty"`
	MaxStamina    int32                  `protobuf:"varint,8,opt,name=max_stamina,json=maxStamina,proto3" json:"max_stamina,omitempty"`
	Avatar        string                 `protobuf:"bytes,9,opt,name=avatar,proto3" json:"avatar,omitempty"`
	SkinId        string                 `protobuf:"bytes,10,opt,name=skin_id,json=skinId,proto3" json:"skin_id,omitempty"`
	Title         string                 `protobuf:"bytes,11,opt,name=title,proto3" json:"title,omitempty"`
	Power         int64                  `protobuf:"varint,12,opt,name=power,proto3" json:"power,omitempty"`
	Alignment     int32                  `protobuf:"varint,13,opt,name=alignment,proto3" json:"alignment,omitempty"`
	unknownFields protoimpl.UnknownFields
	sizeCache     protoimpl.SizeCache
}

func (x *PlayerProfile) Reset() {
	*x = PlayerProfile{}
	mi := &file_player_proto_msgTypes[0]
	ms := protoimpl.X.MessageStateOf(protoimpl.Pointer(x))
	ms.StoreMessageInfo(mi)
}

func (x *PlayerProfile) String() string {
	return protoimpl.X.MessageStringOf(x)
}

func (*PlayerProfile) ProtoMessage() {}

func (x *PlayerProfile) ProtoReflect() protoreflect.Message {
	mi := &file_player_proto_msgTypes[0]
	if x != nil {
		ms := protoimpl.X.MessageStateOf(protoimpl.Pointer(x))
		if ms.LoadMessageInfo() == nil {
			ms.StoreMessageInfo(mi)
		}
		return ms
	}
	return mi.MessageOf(x)
}

func (*PlayerProfile) Descriptor() ([]byte, []int) {
	return file_player_proto_rawDescGZIP(), []int{0}
}

func (x *PlayerProfile) GetUserId() int64 {
	if x != nil {
		return x.UserId
	}
	return 0
}

func (x *PlayerProfile) GetNickname() string {
	if x != nil {
		return x.Nickname
	}
	return ""
}

func (x *PlayerProfile) GetLevel() int32 {
	if x != nil {
		return x.Level
	}
	return 0
}

func (x *PlayerProfile) GetExp() int64 {
	if x != nil {
		return x.Exp
	}
	return 0
}

func (x *PlayerProfile) GetGold() int64 {
	if x != nil {
		return x.Gold
	}
	return 0
}

func (x *PlayerProfile) GetDiamond() int64 {
	if x != nil {
		return x.Diamond
	}
	return 0
}

func (x *PlayerProfile) GetStamina() int32 {
	if x != nil {
		return x.Stamina
	}
	return 0
}

func (x *PlayerProfile) GetMaxStamina() int32 {
	if x != nil {
		return x.MaxStamina
	}
	return 0
}

func (x *PlayerProfile) GetAvatar() string {
	if x != nil {
		return x.Avatar
	}
	return ""
}

func (x *PlayerProfile) GetSkinId() string {
	if x != nil {
		return x.SkinId
	}
	return ""
}

func (x *PlayerProfile) GetTitle() string {
	if x != nil {
		return x.Title
	}
	return ""
}

func (x *PlayerProfile) GetPower() int64 {
	if x != nil {
		return x.Power
	}
	return 0
}

func (x *PlayerProfile) GetAlignment() int32 {
	if x != nil {
		return x.Alignment
	}
	return 0
}

type GetProfileRequest struct {
	state         protoimpl.MessageState `protogen:"open.v1"`
	unknownFields protoimpl.UnknownFields
	sizeCache     protoimpl.SizeCache
}

func (x *GetProfileRequest) Reset() {
	*x = GetProfileRequest{}
	mi := &file_player_proto_msgTypes[1]
	ms := protoimpl.X.MessageStateOf(protoimpl.Pointer(x))
	ms.StoreMessageInfo(mi)
}

func (x *GetProfileRequest) String() string {
	return protoimpl.X.MessageStringOf(x)
}

func (*GetProfileRequest) ProtoMessage() {}

func (x *GetProfileRequest) ProtoReflect() protoreflect.Message {
	mi := &file_player_proto_msgTypes[1]
	if x != nil {
		ms := protoimpl.X.MessageStateOf(protoimpl.Pointer(x))
		if ms.LoadMessageInfo() == nil {
			ms.StoreMessageInfo(mi)
		}
		return ms
	}
	return mi.MessageOf(x)
}

func (*GetProfileRequest) Descriptor() ([]byte, []int) {
	return file_player_proto_rawDescGZIP(), []int{1}
}

type GetPlayerProfileRequest struct {
	state         protoimpl.MessageState `protogen:"open.v1"`
	unknownFields protoimpl.UnknownFields
	sizeCache     protoimpl.SizeCache
}

func (x *GetPlayerProfileRequest) Reset() {
	*x = GetPlayerProfileRequest{}
	mi := &file_player_proto_msgTypes[2]
	ms := protoimpl.X.MessageStateOf(protoimpl.Pointer(x))
	ms.StoreMessageInfo(mi)
}

func (x *GetPlayerProfileRequest) String() string {
	return protoimpl.X.MessageStringOf(x)
}

func (*GetPlayerProfileRequest) ProtoMessage() {}

func (x *GetPlayerProfileRequest) ProtoReflect() protoreflect.Message {
	mi := &file_player_proto_msgTypes[2]
	if x != nil {
		ms := protoimpl.X.MessageStateOf(protoimpl.Pointer(x))
		if ms.LoadMessageInfo() == nil {
			ms.StoreMessageInfo(mi)
		}
		return ms
	}
	return mi.MessageOf(x)
}

func (*GetPlayerProfileRequest) Descriptor() ([]byte, []int) {
	return file_player_proto_rawDescGZIP(), []int{2}
}

type GetProfileResponse struct {
	state         protoimpl.MessageState `protogen:"open.v1"`
	Base          *BaseResponse          `protobuf:"bytes,1,opt,name=base,proto3" json:"base,omitempty"`
	Profile       *PlayerProfile         `protobuf:"bytes,2,opt,name=profile,proto3" json:"profile,omitempty"`
	unknownFields protoimpl.UnknownFields
	sizeCache     protoimpl.SizeCache
}

func (x *GetProfileResponse) Reset() {
	*x = GetProfileResponse{}
	mi := &file_player_proto_msgTypes[3]
	ms := protoimpl.X.MessageStateOf(protoimpl.Pointer(x))
	ms.StoreMessageInfo(mi)
}

func (x *GetProfileResponse) String() string {
	return protoimpl.X.MessageStringOf(x)
}

func (*GetProfileResponse) ProtoMessage() {}

func (x *GetProfileResponse) ProtoReflect() protoreflect.Message {
	mi := &file_player_proto_msgTypes[3]
	if x != nil {
		ms := protoimpl.X.MessageStateOf(protoimpl.Pointer(x))
		if ms.LoadMessageInfo() == nil {
			ms.StoreMessageInfo(mi)
		}
		return ms
	}
	return mi.MessageOf(x)
}

func (*GetProfileResponse) Descriptor() ([]byte, []int) {
	return file_player_proto_rawDescGZIP(), []int{3}
}

func (x *GetProfileResponse) GetBase() *BaseResponse {
	if x != nil {
		return x.Base
	}
	return nil
}

func (x *GetProfileResponse) GetProfile() *PlayerProfile {
	if x != nil {
		return x.Profile
	}
	return nil
}

type GetPlayerProfileResponse struct {
	state         protoimpl.MessageState `protogen:"open.v1"`
	Base          *BaseResponse          `protobuf:"bytes,1,opt,name=base,proto3" json:"base,omitempty"` // C# đang tìm .Base
	Profile       *PlayerProfile         `protobuf:"bytes,2,opt,name=profile,proto3" json:"profile,omitempty"`
	unknownFields protoimpl.UnknownFields
	sizeCache     protoimpl.SizeCache
}

func (x *GetPlayerProfileResponse) Reset() {
	*x = GetPlayerProfileResponse{}
	mi := &file_player_proto_msgTypes[4]
	ms := protoimpl.X.MessageStateOf(protoimpl.Pointer(x))
	ms.StoreMessageInfo(mi)
}

func (x *GetPlayerProfileResponse) String() string {
	return protoimpl.X.MessageStringOf(x)
}

func (*GetPlayerProfileResponse) ProtoMessage() {}

func (x *GetPlayerProfileResponse) ProtoReflect() protoreflect.Message {
	mi := &file_player_proto_msgTypes[4]
	if x != nil {
		ms := protoimpl.X.MessageStateOf(protoimpl.Pointer(x))
		if ms.LoadMessageInfo() == nil {
			ms.StoreMessageInfo(mi)
		}
		return ms
	}
	return mi.MessageOf(x)
}

func (*GetPlayerProfileResponse) Descriptor() ([]byte, []int) {
	return file_player_proto_rawDescGZIP(), []int{4}
}

func (x *GetPlayerProfileResponse) GetBase() *BaseResponse {
	if x != nil {
		return x.Base
	}
	return nil
}

func (x *GetPlayerProfileResponse) GetProfile() *PlayerProfile {
	if x != nil {
		return x.Profile
	}
	return nil
}

type CreatePlayerRequest struct {
	state         protoimpl.MessageState `protogen:"open.v1"`
	Nickname      string                 `protobuf:"bytes,1,opt,name=nickname,proto3" json:"nickname,omitempty"`
	unknownFields protoimpl.UnknownFields
	sizeCache     protoimpl.SizeCache
}

func (x *CreatePlayerRequest) Reset() {
	*x = CreatePlayerRequest{}
	mi := &file_player_proto_msgTypes[5]
	ms := protoimpl.X.MessageStateOf(protoimpl.Pointer(x))
	ms.StoreMessageInfo(mi)
}

func (x *CreatePlayerRequest) String() string {
	return protoimpl.X.MessageStringOf(x)
}

func (*CreatePlayerRequest) ProtoMessage() {}

func (x *CreatePlayerRequest) ProtoReflect() protoreflect.Message {
	mi := &file_player_proto_msgTypes[5]
	if x != nil {
		ms := protoimpl.X.MessageStateOf(protoimpl.Pointer(x))
		if ms.LoadMessageInfo() == nil {
			ms.StoreMessageInfo(mi)
		}
		return ms
	}
	return mi.MessageOf(x)
}

func (*CreatePlayerRequest) Descriptor() ([]byte, []int) {
	return file_player_proto_rawDescGZIP(), []int{5}
}

func (x *CreatePlayerRequest) GetNickname() string {
	if x != nil {
		return x.Nickname
	}
	return ""
}

type CreatePlayerResponse struct {
	state         protoimpl.MessageState `protogen:"open.v1"`
	Base          *BaseResponse          `protobuf:"bytes,1,opt,name=base,proto3" json:"base,omitempty"`
	Profile       *PlayerProfile         `protobuf:"bytes,2,opt,name=profile,proto3" json:"profile,omitempty"`
	unknownFields protoimpl.UnknownFields
	sizeCache     protoimpl.SizeCache
}

func (x *CreatePlayerResponse) Reset() {
	*x = CreatePlayerResponse{}
	mi := &file_player_proto_msgTypes[6]
	ms := protoimpl.X.MessageStateOf(protoimpl.Pointer(x))
	ms.StoreMessageInfo(mi)
}

func (x *CreatePlayerResponse) String() string {
	return protoimpl.X.MessageStringOf(x)
}

func (*CreatePlayerResponse) ProtoMessage() {}

func (x *CreatePlayerResponse) ProtoReflect() protoreflect.Message {
	mi := &file_player_proto_msgTypes[6]
	if x != nil {
		ms := protoimpl.X.MessageStateOf(protoimpl.Pointer(x))
		if ms.LoadMessageInfo() == nil {
			ms.StoreMessageInfo(mi)
		}
		return ms
	}
	return mi.MessageOf(x)
}

func (*CreatePlayerResponse) Descriptor() ([]byte, []int) {
	return file_player_proto_rawDescGZIP(), []int{6}
}

func (x *CreatePlayerResponse) GetBase() *BaseResponse {
	if x != nil {
		return x.Base
	}
	return nil
}

func (x *CreatePlayerResponse) GetProfile() *PlayerProfile {
	if x != nil {
		return x.Profile
	}
	return nil
}

var File_player_proto protoreflect.FileDescriptor

const file_player_proto_rawDesc = "" +
	"\n" +
	"\fplayer.proto\x12\x02pb\x1a\fcommon.proto\"\xd0\x02\n" +
	"\rPlayerProfile\x12\x17\n" +
	"\auser_id\x18\x01 \x01(\x03R\x06userId\x12\x1a\n" +
	"\bnickname\x18\x02 \x01(\tR\bnickname\x12\x14\n" +
	"\x05level\x18\x03 \x01(\x05R\x05level\x12\x10\n" +
	"\x03exp\x18\x04 \x01(\x03R\x03exp\x12\x12\n" +
	"\x04gold\x18\x05 \x01(\x03R\x04gold\x12\x18\n" +
	"\adiamond\x18\x06 \x01(\x03R\adiamond\x12\x18\n" +
	"\astamina\x18\a \x01(\x05R\astamina\x12\x1f\n" +
	"\vmax_stamina\x18\b \x01(\x05R\n" +
	"maxStamina\x12\x16\n" +
	"\x06avatar\x18\t \x01(\tR\x06avatar\x12\x17\n" +
	"\askin_id\x18\n" +
	" \x01(\tR\x06skinId\x12\x14\n" +
	"\x05title\x18\v \x01(\tR\x05title\x12\x14\n" +
	"\x05power\x18\f \x01(\x03R\x05power\x12\x1c\n" +
	"\talignment\x18\r \x01(\x05R\talignment\"\x13\n" +
	"\x11GetProfileRequest\"\x19\n" +
	"\x17GetPlayerProfileRequest\"g\n" +
	"\x12GetProfileResponse\x12$\n" +
	"\x04base\x18\x01 \x01(\v2\x10.pb.BaseResponseR\x04base\x12+\n" +
	"\aprofile\x18\x02 \x01(\v2\x11.pb.PlayerProfileR\aprofile\"m\n" +
	"\x18GetPlayerProfileResponse\x12$\n" +
	"\x04base\x18\x01 \x01(\v2\x10.pb.BaseResponseR\x04base\x12+\n" +
	"\aprofile\x18\x02 \x01(\v2\x11.pb.PlayerProfileR\aprofile\"1\n" +
	"\x13CreatePlayerRequest\x12\x1a\n" +
	"\bnickname\x18\x01 \x01(\tR\bnickname\"i\n" +
	"\x14CreatePlayerResponse\x12$\n" +
	"\x04base\x18\x01 \x01(\v2\x10.pb.BaseResponseR\x04base\x12+\n" +
	"\aprofile\x18\x02 \x01(\v2\x11.pb.PlayerProfileR\aprofileB*Z\x10server/pkg/pb;pb\xaa\x02\x15GameClient.Network.Pbb\x06proto3"

var (
	file_player_proto_rawDescOnce sync.Once
	file_player_proto_rawDescData []byte
)

func file_player_proto_rawDescGZIP() []byte {
	file_player_proto_rawDescOnce.Do(func() {
		file_player_proto_rawDescData = protoimpl.X.CompressGZIP(unsafe.Slice(unsafe.StringData(file_player_proto_rawDesc), len(file_player_proto_rawDesc)))
	})
	return file_player_proto_rawDescData
}

var file_player_proto_msgTypes = make([]protoimpl.MessageInfo, 7)
var file_player_proto_goTypes = []any{
	(*PlayerProfile)(nil),            // 0: pb.PlayerProfile
	(*GetProfileRequest)(nil),        // 1: pb.GetProfileRequest
	(*GetPlayerProfileRequest)(nil),  // 2: pb.GetPlayerProfileRequest
	(*GetProfileResponse)(nil),       // 3: pb.GetProfileResponse
	(*GetPlayerProfileResponse)(nil), // 4: pb.GetPlayerProfileResponse
	(*CreatePlayerRequest)(nil),      // 5: pb.CreatePlayerRequest
	(*CreatePlayerResponse)(nil),     // 6: pb.CreatePlayerResponse
	(*BaseResponse)(nil),             // 7: pb.BaseResponse
}
var file_player_proto_depIdxs = []int32{
	7, // 0: pb.GetProfileResponse.base:type_name -> pb.BaseResponse
	0, // 1: pb.GetProfileResponse.profile:type_name -> pb.PlayerProfile
	7, // 2: pb.GetPlayerProfileResponse.base:type_name -> pb.BaseResponse
	0, // 3: pb.GetPlayerProfileResponse.profile:type_name -> pb.PlayerProfile
	7, // 4: pb.CreatePlayerResponse.base:type_name -> pb.BaseResponse
	0, // 5: pb.CreatePlayerResponse.profile:type_name -> pb.PlayerProfile
	6, // [6:6] is the sub-list for method output_type
	6, // [6:6] is the sub-list for method input_type
	6, // [6:6] is the sub-list for extension type_name
	6, // [6:6] is the sub-list for extension extendee
	0, // [0:6] is the sub-list for field type_name
}

func init() { file_player_proto_init() }
func file_player_proto_init() {
	if File_player_proto != nil {
		return
	}
	file_common_proto_init()
	type x struct{}
	out := protoimpl.TypeBuilder{
		File: protoimpl.DescBuilder{
			GoPackagePath: reflect.TypeOf(x{}).PkgPath(),
			RawDescriptor: unsafe.Slice(unsafe.StringData(file_player_proto_rawDesc), len(file_player_proto_rawDesc)),
			NumEnums:      0,
			NumMessages:   7,
			NumExtensions: 0,
			NumServices:   0,
		},
		GoTypes:           file_player_proto_goTypes,
		DependencyIndexes: file_player_proto_depIdxs,
		MessageInfos:      file_player_proto_msgTypes,
	}.Build()
	File_player_proto = out.File
	file_player_proto_goTypes = nil
	file_player_proto_depIdxs = nil
}
