package profanity

import "testing"

func TestProfanity_Contains(t *testing.T) {
	tests := []struct {
		input    string
		expected bool
	}{
		{"hello world", false},
		{"fuck you", true},
		{"FUCK", true},
		{"f.u.c.k", true},
		{"sh1t", true},
		{"địt", true},
		{"vcl", true},
	}

	for _, tt := range tests {
		if res := Contains(tt.input); res != tt.expected {
			t.Errorf("Contains('%s') = %v, expected %v", tt.input, res, tt.expected)
		}
	}
}

func TestProfanity_Filter(t *testing.T) {
	tests := []struct {
		input    string
		expected string
	}{
		{"hello", "hello"},
		{"fuck you", "*****you"},
		{"F.u.c.k", "*******"},
		{"đm nó", "***nó"},
	}

	for _, tt := range tests {
		if res := Filter(tt.input); res != tt.expected {
			t.Errorf("Filter('%s') = '%s', expected '%s'", tt.input, res, tt.expected)
		}
	}
}
