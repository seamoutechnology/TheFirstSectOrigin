package email

import (
	"fmt"
	"net/smtp"
)

type Mailer struct {
	Host     string
	Port     int
	Username string
	Password string
}

func NewMailer(host string, port int, username, password string) *Mailer {
	return &Mailer{
		Host:     host,
		Port:     port,
		Username: username,
		Password: password,
	}
}

func (m *Mailer) Send(to, subject, body string) error {
	addr := fmt.Sprintf("%s:%d", m.Host, m.Port)
	auth := smtp.PlainAuth("", m.Username, m.Password, m.Host)
	
	msg := fmt.Sprintf("To: %s\r\nSubject: %s\r\n\r\n%s", to, subject, body)
	
	return smtp.SendMail(addr, auth, m.Username, []string{to}, []byte(msg))
}
